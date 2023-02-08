using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using Minio.DataModel.ObjectLock;

namespace Minio.DataModel;

public class CreateMultipartUploadArgs<T> : ObjectWriteArgs<T>
    where T : CreateMultipartUploadArgs<T>
{
    internal CreateMultipartUploadArgs()
    {
        RequestMethod = HttpMethod.Post;
    }

    internal RetentionMode ObjectLockRetentionMode { get; set; }
    internal DateTime RetentionUntilDate { get; set; }
    internal bool ObjectLockSet { get; set; }

    public CreateMultipartUploadArgs<T> WithObjectLockMode(RetentionMode mode)
    {
        ObjectLockSet = true;
        ObjectLockRetentionMode = mode;
        return this;
    }

    public CreateMultipartUploadArgs<T> WithObjectLockRetentionDate(DateTime untilDate)
    {
        ObjectLockSet = true;
        RetentionUntilDate = new DateTime(untilDate.Year, untilDate.Month, untilDate.Day,
            untilDate.Hour, untilDate.Minute, untilDate.Second);
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("uploads", "");
        if (ObjectLockSet)
        {
            if (!RetentionUntilDate.Equals(default))
                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-retain-until-date",
                    utils.To8601String(RetentionUntilDate));
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-mode",
                ObjectLockRetentionMode == RetentionMode.GOVERNANCE ? "GOVERNANCE" : "COMPLIANCE");
        }

        requestMessageBuilder.AddOrUpdateHeaderParameter("content-type", ContentType);

        return requestMessageBuilder;
    }
}

public class CreateMultipartUploadArgs : CreateMultipartUploadArgs<CreateMultipartUploadArgs>
{
    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("uploads", "");

        if (ObjectTags != null && ObjectTags.TaggingSet != null
                               && ObjectTags.TaggingSet.Tag.Count > 0)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging", ObjectTags.GetTagString());

        requestMessageBuilder.AddOrUpdateHeaderParameter("content-type", ContentType);

        return requestMessageBuilder;
    }
}

public class CreateMultipartUploadResponse : GenericResponse
{
    internal CreateMultipartUploadResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        InitiateMultipartUploadResult newUpload = null;
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
        {
            newUpload = (InitiateMultipartUploadResult)new XmlSerializer(typeof(InitiateMultipartUploadResult))
                .Deserialize(stream);
        }

        UploadId = newUpload.UploadId;
    }

    public string UploadId { get; }
}

public class SignObjectPartArgs : ObjectWriteArgs<SignObjectPartArgs>
{
    public SignObjectPartArgs()
    {
        RequestMethod = HttpMethod.Put;
        RequestBody = null;
        PartNumber = 0;
        ContentType = "application/octet-stream";
    }

    public string UploadId { get;  set; }
    public int PartNumber { get; set; }
    public string FileName { get; set; }
    public long ObjectSize { get; set; }

    internal override void Validate()
    {
        base.Validate();
        // Check atleast one of filename or stream are initialized
        if (string.IsNullOrWhiteSpace(FileName))
            throw new ArgumentException(nameof(FileName) +
                                        " must be set.");
        if (PartNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(PartNumber), PartNumber,
                "Invalid Part number value. Cannot be less than 0");

        if (!string.IsNullOrWhiteSpace(FileName)) utils.ValidateFile(FileName);
        // Check object size when using stream data

        Populate();
        if (string.IsNullOrWhiteSpace(UploadId))
            throw new ArgumentNullException(nameof(UploadId) + " not assigned for PutObjectPart operation.");
    }

    private void Populate()
    {
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder = base.BuildRequest(requestMessageBuilder);
        if (string.IsNullOrWhiteSpace(ContentType)) ContentType = "application/octet-stream";
        if (!Headers.ContainsKey("Content-Type")) Headers["Content-Type"] = ContentType;

        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Type", Headers["Content-Type"]);
        if (!string.IsNullOrWhiteSpace(UploadId) && PartNumber > 0)
        {
            requestMessageBuilder.AddQueryParameter("uploadId", $"{UploadId}");
            requestMessageBuilder.AddQueryParameter("partNumber", $"{PartNumber}");
        }

        if (ObjectTags != null && ObjectTags.TaggingSet != null
                               && ObjectTags.TaggingSet.Tag.Count > 0)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging", ObjectTags.GetTagString());
        if (Retention != null)
        {
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-retain-until-date",
                Retention.RetainUntilDate);
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-mode", Retention.Mode.ToString());
            requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
                utils.getMD5SumStr(RequestBody));
        }

        if (LegalHoldEnabled != null)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-legal-hold",
                LegalHoldEnabled == true ? "ON" : "OFF");
        if (RequestBody != null)
        {
            var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(RequestBody);
            var hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-content-sha256", hex);
            requestMessageBuilder.SetBody(RequestBody);
        }

        return requestMessageBuilder;
    }

    public new SignObjectPartArgs WithHeaders(Dictionary<string, string> metaData)
    {
        var sseHeaders = new Dictionary<string, string>();
        Headers = Headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (metaData != null)
            foreach (var p in metaData)
            {
                var key = p.Key;
                if (!OperationsUtil.IsSupportedHeader(p.Key) &&
                    !p.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase) &&
                    !OperationsUtil.IsSSEHeader(p.Key))
                {
                    key = "x-amz-meta-" + key.ToLowerInvariant();
                    Headers.Remove(p.Key);
                }

                Headers[key] = p.Value;
                if (key == "Content-Type")
                    ContentType = p.Value;
            }

        if (string.IsNullOrWhiteSpace(ContentType)) ContentType = "application/octet-stream";
        if (!Headers.ContainsKey("Content-Type")) Headers["Content-Type"] = ContentType;
        return this;
    }


    public SignObjectPartArgs WithUploadId(string id = null)
    {
        UploadId = id;
        return this;
    }

    public SignObjectPartArgs WithPartNumber(int num)
    {
        PartNumber = num;
        return this;
    }

    public SignObjectPartArgs WithFileName(string file)
    {
        FileName = file;
        return this;
    }

    public SignObjectPartArgs WithObjectSize(long size)
    {
        ObjectSize = size;
        return this;
    }
}