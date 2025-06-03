using System.Text.Json.Serialization;

namespace Minio.Admin.DataModel.Result;
public class AdminListServiceAccountsResult
{
    [JsonPropertyName("accounts")]
    public IList<AdminServiceAccountInfo> Accounts { get; set; } 
}
