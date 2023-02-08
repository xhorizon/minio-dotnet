using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Minio.DataModel;

public class UploadPartSignResult
{
    public HttpMethod Method { get; set; } = HttpMethod.Put;
    public Uri RequestUri { get; set; }
    public Dictionary<string, string> Headers { get; set; }
}