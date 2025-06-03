using System.Text.Json.Serialization;

namespace Minio.Admin.DataModel;
public class AdminServiceAccountInfo
{
    [JsonPropertyName("parentUser")]
    public string ParentUser { get; set; } = string.Empty;
    [JsonPropertyName("accountStatus")]
    public string AccountStatus { get; set; } = string.Empty;
    [JsonPropertyName("impliedPolicy")]
    public bool ImpliedPolicy { get; set; }
    [JsonPropertyName("accessKey")]
    public string AccessKey { get; set; } = string.Empty;
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    [JsonPropertyName("expiration")]
    public DateTimeOffset? Expiration { get; set; }  
}
