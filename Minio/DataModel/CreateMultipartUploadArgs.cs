using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
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

public class SignObjectPartArgs : PutObjectArgs
{
    public SignObjectPartArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal override void Validate()
    {
        base.Validate();
        if (string.IsNullOrWhiteSpace(UploadId))
            throw new ArgumentNullException(nameof(UploadId) + " not assigned for PutObjectPart operation.");
    }

    public new PutObjectPartArgs WithBucket(string bkt)
    {
        return (PutObjectPartArgs)base.WithBucket(bkt);
    }

    public new PutObjectPartArgs WithObject(string obj)
    {
        return (PutObjectPartArgs)base.WithObject(obj);
    }

    public new PutObjectPartArgs WithObjectSize(long size)
    {
        return (PutObjectPartArgs)base.WithObjectSize(size);
    }

    public new PutObjectPartArgs WithHeaders(Dictionary<string, string> hdr)
    {
        return (PutObjectPartArgs)base.WithHeaders(hdr);
    }

    public PutObjectPartArgs WithRequestBody(object data)
    {
        return (PutObjectPartArgs)base.WithRequestBody(utils.ObjectToByteArray(data));
    }

    public new PutObjectPartArgs WithStreamData(Stream data)
    {
        return (PutObjectPartArgs)base.WithStreamData(data);
    }

    public new PutObjectPartArgs WithContentType(string type)
    {
        return (PutObjectPartArgs)base.WithContentType(type);
    }

    public new PutObjectPartArgs WithUploadId(string id)
    {
        return (PutObjectPartArgs)base.WithUploadId(id);
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        return requestMessageBuilder;
    }
}