using System.Diagnostics.CodeAnalysis;
using Minio.ApiEndpoints;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Minio.DataModel.Result;

namespace Minio;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "Split up in partial classes")]
public partial class MinioClient : IMultipartUploadOperations
{
    public Task AbortMultipartUploadAsync(string bucketName, string key, string uploadId, CancellationToken cToken = default)
    {
        var rmArgs = new RemoveUploadArgs()
         .WithBucket(bucketName)
         .WithObject(key)
         .WithUploadId(uploadId);
        return RemoveUploadAsync(rmArgs, cToken);

    }

    public async Task<CreateMultipartUploadResponse> CreateMultipartUploadAsync(CreateMultipartUploadArgs args, CancellationToken cToken = default)
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
        var requestMessageBuilder = await this.CreateRequest(multipartUploadArgs).ConfigureAwait(false);
        using var response = await this.ExecuteTaskAsync(ResponseErrorHandlers,requestMessageBuilder,cancellationToken:cToken).ConfigureAwait(false);
        var uploadResponse = new CreateMultipartUploadResponse(response.StatusCode, response.Content);
        return uploadResponse;
    }

    public async Task<FinishMultipartUploadResponse> FinishMultipartUploadAsync(FinishMultipartUploadArgs args, CancellationToken cToken = default)
    {
        args.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response = await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder, cancellationToken: cToken).ConfigureAwait(false);
     
        var ret = new FinishMultipartUploadResponse(response.StatusCode, response.Content);
        return ret;
    }

    public async Task<MultipartUploadPartSignResult> SignMultipartUploadPartAsync(SignObjectPartArgs args)
    {
        if (args.PartNumber <= 0)
        {
            throw new InvalidOperationException("partNum can not <= `0`");
        }

        args.Validate();


        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);

        // var startTime = DateTime.Now;
        var v4Authenticator = new V4Authenticator(Config.Secure,
            Config.AccessKey, Config.SecretKey, Config.Region,
            Config.SessionToken);
        requestMessageBuilder.AddOrUpdateHeaderParameter("Authorization",
            v4Authenticator.Authenticate(requestMessageBuilder));

        var res = new MultipartUploadPartSignResult
        {
            Headers = requestMessageBuilder.HeaderParameters,
            RequestUri = requestMessageBuilder.RequestUri,
            Method = requestMessageBuilder.Method
        };

        return res;
    }
}
