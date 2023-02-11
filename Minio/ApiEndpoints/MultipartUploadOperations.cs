using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
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

    public async Task<UploadPartSignResult> SignMultipartUploadPartAsync(SignObjectPartArgs args)
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

    public async Task<FinishedMultipartUploadResponse> FinishedMultipartUploadAsync(FinishedMultipartUploadArgs args,
        CancellationToken cancellationToken = default)
    {
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
            .ConfigureAwait(false);
        var ret = new FinishedMultipartUploadResponse(response.StatusCode, response.Content);
        return ret;
    }
}