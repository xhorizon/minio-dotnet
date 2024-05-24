using System.Net;
using System.Text;
using CommunityToolkit.HighPerformance;
using Minio.DataModel.Result;
using Minio.Helper;

namespace Minio.DataModel.Response;
public class FinishMultipartUploadResponse : GenericResponse
{
    public HttpStatusCode StatusCode => ResponseStatusCode;
    public string Content => ResponseContent;

    public string ETag => result.ETag;
    public string Bucket => result.Bucket;
    public string Key => result.Key;

    private readonly FinishMultipartUploadResult result;

    public FinishMultipartUploadResponse(HttpStatusCode statusCode, string responseContent) : base(statusCode, responseContent)
    {
        using var stream = Encoding.UTF8.GetBytes(responseContent).AsMemory().AsStream();
        result = Utils.DeserializeXml<FinishMultipartUploadResult>(stream);

    }
}
