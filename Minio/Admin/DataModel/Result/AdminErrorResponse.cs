using System.Text.Json.Serialization;

namespace Minio.Admin.DataModel.Result;
public class AdminErrorResponse
{
    [JsonPropertyName("Code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("Resource")]
    public string Resource { get; set; } = string.Empty;

    [JsonPropertyName("RequestId")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("HostId")]
    public string HostId { get; set; } = string.Empty;
}
