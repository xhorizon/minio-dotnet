using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Minio.DataModel.Result;

namespace Minio.ApiEndpoints;

public interface IMultipartUploadOperations
{
    Task<CreateMultipartUploadResponse> CreateMultipartUploadAsync(CreateMultipartUploadArgs args,
        CancellationToken cancellationToken = default);

    Task AbortMultipartUploadAsync(string bucketName, string key, string uploadId,
        CancellationToken cancellationToken = default);

    Task<MultipartUploadPartSignResult> SignMultipartUploadPartAsync(SignObjectPartArgs args);

    Task<FinishMultipartUploadResponse> FinishMultipartUploadAsync(FinishMultipartUploadArgs args, CancellationToken cancellationToken = default);
}
