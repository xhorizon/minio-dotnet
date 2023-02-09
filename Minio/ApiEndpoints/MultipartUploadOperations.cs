using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

    public async Task<UploadPartSignResult> SignMultipartUploadPart(SignObjectPartArgs args)
    {
        if (args.PartNumber <= 0)
        {
            throw new ArgumentException("partNum can not <= `0`");
        }

        ArgsCheck(args);


        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);

        // var startTime = DateTime.Now;
        var v4Authenticator = new V4Authenticator(Secure,
            AccessKey, SecretKey, Region,
            SessionToken);
        requestMessageBuilder.AddOrUpdateHeaderParameter("Authorization",
            v4Authenticator.Authenticate(requestMessageBuilder));

        var res = new UploadPartSignResult
        {
            Headers = requestMessageBuilder.HeaderParameters,
            RequestUri = requestMessageBuilder.RequestUri,
            Method = requestMessageBuilder.Method
        };

        return res;
    }

    public Task FinishedMultipartUploadAsync(FinishedMultipartUploadArgs args,
        CancellationToken cancellationToken = default)
    {
        var aa = new CompleteMultipartUploadArgs
        {
            UploadId = args.UploadId,
            // destBucketName, destObjectName, metadata, sseHeaders
            RequestMethod = HttpMethod.Post,
            BucketName = args.BucketName,
            ObjectName = args.ObjectName,
            Headers = new Dictionary<string, string>(),
            SSE = args.SSE,
        };

        aa.WithETags(args.ETags);
        
        aa.SSE?.Marshal(args.Headers);
        
        if (args.Headers is { Count: > 0 })
        {
            aa.Headers = aa.Headers.Concat(args.Headers).GroupBy(item => item.Key)
                .ToDictionary(item => item.Key, item => item.First().Value);
        }

        return CompleteMultipartUploadAsync(aa, cancellationToken);
    }
}