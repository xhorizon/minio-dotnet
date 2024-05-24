namespace Minio.DataModel.Result;
public class MultipartUploadPartSignResult
{
    public HttpMethod Method { get; set; } = HttpMethod.Put;
    public Uri RequestUri { get; set; }
    public IDictionary<string, string> Headers { get; set; }
}
