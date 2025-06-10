using System.Text.Json.Serialization;
#nullable enable
namespace Minio.Admin.DataModel.Result;
public class AdminAddServiceAccountResult
{
    [JsonPropertyName("credentials")]
    public AdminCredential? Credentials { get; set; } 
}
