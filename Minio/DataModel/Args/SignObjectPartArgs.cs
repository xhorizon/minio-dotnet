using System.Security.Cryptography;
using Minio.Helper;

namespace Minio.DataModel.Args;
public class SignObjectPartArgs : ObjectWriteArgs<SignObjectPartArgs>
{
    public SignObjectPartArgs()
    {
        RequestMethod = HttpMethod.Put;
        RequestBody = null;
        PartNumber = 0;
        ContentType = "application/octet-stream";
    }

    public string UploadId { get; set; }
    public int PartNumber { get; set; }

    internal override void Validate()
    {
        base.Validate();
   
        if (string.IsNullOrWhiteSpace(UploadId))
            throw new InvalidOperationException(nameof(UploadId) + " must be set.");
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
                Utils.GetMD5SumStr(RequestBody.Span));
        }

        if (LegalHoldEnabled != null)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-legal-hold",
                LegalHoldEnabled == true ? "ON" : "OFF");
        if (!RequestBody.IsEmpty)
        {
#if NETSTANDARD
            using var sha = SHA256.Create();
            var hash
                = sha.ComputeHash(RequestBody.ToArray());
#else
            var hash = SHA256.HashData(RequestBody.Span);
#endif
            var hex = BitConverter.ToString(hash).Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
                .ToLowerInvariant();
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-content-sha256", hex);
            requestMessageBuilder.SetBody(RequestBody);
        }

        return requestMessageBuilder;
    }

    public new SignObjectPartArgs WithHeaders(IDictionary<string, string> headers)
    {
        Headers ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (headers is not null)
            foreach (var p in headers)
            {
                var key = p.Key;
                if (!OperationsUtil.IsSupportedHeader(p.Key) &&
                    !p.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase) &&
                    !OperationsUtil.IsSSEHeader(p.Key))
                {
                    key = "x-amz-meta-" + key.ToLowerInvariant();
                    _ = Headers.Remove(p.Key);
                }

                Headers[key] = p.Value;
                if (string.Equals(key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                    ContentType = p.Value;
            }

        if (string.IsNullOrWhiteSpace(ContentType)) ContentType = "application/octet-stream";
        Headers["Content-Type"] = ContentType;
        return this;
    }

    public SignObjectPartArgs WithContentSha256(string sha)
    {
        Headers["x-amz-content-sha256"] = sha;
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



}
