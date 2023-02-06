using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Minio.DataModel;

public class UploadPartSignResult
{
    public HttpMethod Method => _message.Method;
    public Uri RequestUri => _message.RequestUri;
    public HttpHeaders Headers => _message.Headers;

    public HttpRequestMessage Raw => _message;
    private readonly HttpRequestMessage _message;

    public UploadPartSignResult(HttpRequestMessage message)
    {
        _message = message;
    }
}