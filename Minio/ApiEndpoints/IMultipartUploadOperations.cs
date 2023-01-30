using System.Threading;
using System.Threading.Tasks;
using Minio.DataModel;

namespace Minio;

public interface IMultipartUploadOperations
{
    Task<CreateMultipartUploadResponse> CreateMultipartUploadAsync(CreateMultipartUploadArgs args,
        CancellationToken cancellationToken = default);

    Task AbortMultipartUploadAsync(string bucketName, string key, string uploadId,
        CancellationToken cancellationToken = default);
}