using System.Text.Json.Serialization;

namespace Minio.Admin.DataModel.Result;

/// <summary>
/// AdminErrorResponse
/// </summary>
public class AdminErrorResponse
{
    /// <summary>
    /// Code
    /// </summary>
    [JsonPropertyName("Code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Message
    /// </summary>
    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Resource
    /// </summary>
    [JsonPropertyName("Resource")]
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// RequestId
    /// </summary>
    [JsonPropertyName("RequestId")]
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// HostId
    /// </summary>
    [JsonPropertyName("HostId")]
    public string HostId { get; set; } = string.Empty;
}
