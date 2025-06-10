#nullable enable
using System.Text.Json.Serialization;

namespace Minio.Admin.DataModel;
public class AdminCredential
{
    [JsonPropertyName("accessKey")]
    public string AccessKey { get; set; } = string.Empty;
    [JsonPropertyName("secretKey")]
    public string SecretKey { get; set; } = string.Empty;
    [JsonPropertyName("sessionToken")]
    public string? SessionToken { get; set; } 
    [JsonPropertyName("expiration")]
    public DateTimeOffset Expiration { get; set; }
}
