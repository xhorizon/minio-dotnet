#nullable enable
using System.Text.Json.Serialization;

namespace Minio.Admin.DataModel;
public class AdminUserAuthInfo
{
    [JsonPropertyName("type")]
    public string UserAuthType { get; set; } = string.Empty;
    [JsonPropertyName("authServer")]
    public string? AuthServer { get; set; }
    [JsonPropertyName("authServerUserID")]
    public string? AuthServerUserID { get; set; }

}
