using System.Text.Json.Serialization;

namespace Minio.Admin.DataModel;
/// <summary>
/// AdminServiceAccountInfo
/// </summary>
public class AdminServiceAccountInfo
{
    /// <summary>
    /// ParentUser
    /// </summary>
    [JsonPropertyName("parentUser")]
    public string ParentUser { get; set; } = string.Empty;
    /// <summary>
    /// AccountStatus
    /// </summary>
    [JsonPropertyName("accountStatus")]
    public string AccountStatus { get; set; } = string.Empty;
    /// <summary>
    /// ImpliedPolicy
    /// </summary>
    [JsonPropertyName("impliedPolicy")]
    public bool ImpliedPolicy { get; set; }
    /// <summary>
    /// AccessKey
    /// </summary>
    [JsonPropertyName("accessKey")]
    public string AccessKey { get; set; } = string.Empty;
    /// <summary>
    /// Name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Description
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Expiration
    /// </summary>
    [JsonPropertyName("expiration")]
    public DateTimeOffset? Expiration { get; set; }  
}
