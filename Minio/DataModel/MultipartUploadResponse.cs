#nullable enable
using System.Linq;
using System.Net;
using System.Xml.Linq;

namespace Minio.DataModel;

public class FinishedMultipartUploadResponse : GenericResponse
{
    public HttpStatusCode StatusCode => ResponseStatusCode;
    public string Content => ResponseContent;

    public string ETag => _eTag ??= GetElValue("ETag");
    public string Bucket => _bucket ??= GetElValue("Bucket");
    public string Key => _key ??= GetElValue("Key");

    private string? _eTag;
    private string? _bucket;
    private string? _key;
    private XDocument? _document;

    internal FinishedMultipartUploadResponse(HttpStatusCode statusCode, string responseContent) : base(statusCode,
        responseContent)
    {
    }


    string GetElValue(string k)
    {
        if (_document == null)
        {
            _document = XDocument.Parse(Content);
        }

        if (_document.Root == null) return string.Empty;
        var el = _document.Root.Descendants($"{{http://s3.amazonaws.com/doc/2006-03-01/}}{k}").FirstOrDefault();
        return el?.Value ?? string.Empty;
    }
}