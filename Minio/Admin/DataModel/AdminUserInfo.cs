#nullable enable

using System.Text.Json.Serialization;

namespace Minio.Admin.DataModel;

public class AdminUserInfo
{
    [JsonPropertyName("userAuthInfo")]
    public AdminUserAuthInfo? AuthInfo { get; set; }

    [JsonPropertyName("secretKey")]
    public string? SecretKey { get; set; }

    [JsonPropertyName("policyName")]
    public string? PolicyName { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("memberOf")]
    public IList<string> MemberOf { get; set; } = [];

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; set; }
}
