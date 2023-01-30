using System.Threading;
using System.Threading.Tasks;
using Minio.DataModel;

namespace Minio;

public partial class MinioClient : IMultipartUploadOperations
{
    public async Task<CreateMultipartUploadResponse> CreateMultipartUploadAsync(CreateMultipartUploadArgs args,
        CancellationToken cancellationToken = default)
    {
        var multipartUploadArgs = new NewMultipartUploadPutArgs()
            .WithBucket(args.BucketName)
            .WithObject(args.ObjectName)
            .WithVersionId(args.VersionId)
            .WithHeaders(args.Headers)
            .WithContentType(args.ContentType)
            .WithTagging(args.ObjectTags)
            .WithLegalHold(args.LegalHoldEnabled)
            .WithRetentionConfiguration(args.Retention)
            .WithServerSideEncryption(args.SSE);

        multipartUploadArgs.Validate();
        var requestMessageBuilder = await CreateRequest(multipartUploadArgs).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken);
        var uploadResponse = new CreateMultipartUploadResponse(response.StatusCode, response.Content);
        return uploadResponse;
    }

    public Task AbortMultipartUploadAsync(string bucketName, string key, string uploadId,
        CancellationToken cancellationToken = default)
    {
        var rmArgs = new RemoveUploadArgs()
            .WithBucket(bucketName)
            .WithObject(key)
            .WithUploadId(uploadId);
        return RemoveUploadAsync(rmArgs, cancellationToken);
    }
}