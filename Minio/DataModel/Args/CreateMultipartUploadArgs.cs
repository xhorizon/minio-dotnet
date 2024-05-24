using Minio.DataModel.ObjectLock;
using Minio.Helper;

namespace Minio.DataModel.Args;

/// <summary>
/// COPY from <see cref="NewMultipartUploadArgs{T}"></see>
/// </summary>
/// <typeparam name="T"></typeparam>
public class CreateMultipartUploadArgs<T> : ObjectWriteArgs<T> where T : CreateMultipartUploadArgs<T>
{
    internal CreateMultipartUploadArgs()
    {
        RequestMethod = HttpMethod.Post;
    }

    internal ObjectRetentionMode ObjectLockRetentionMode { get; set; }
    internal DateTime RetentionUntilDate { get; set; }
    internal bool ObjectLockSet { get; set; }

    public CreateMultipartUploadArgs<T> WithObjectLockMode(ObjectRetentionMode mode)
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
                    Utils.To8601String(RetentionUntilDate));

            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-mode",
                ObjectLockRetentionMode == ObjectRetentionMode.GOVERNANCE ? "GOVERNANCE" : "COMPLIANCE");
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

