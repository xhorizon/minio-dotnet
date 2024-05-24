using System.Net;
using System.Text;
using CommunityToolkit.HighPerformance;
using Minio.DataModel.Result;
using Minio.Helper;

namespace Minio.DataModel.Response;
public class CreateMultipartUploadResponse : GenericResponse
{
    internal CreateMultipartUploadResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        using var stream = Encoding.UTF8.GetBytes(responseContent).AsMemory().AsStream();
        var newUpload = Utils.DeserializeXml<InitiateMultipartUploadResult>(stream);

        UploadId = newUpload.UploadId;
    }

    internal string UploadId { get; }
}
