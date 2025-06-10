#nullable enable

using System.Text.Json;
using Minio.Admin.DataModel.Args;
using Minio.Admin.DataModel.Result;
using Minio.DataModel.Result;

namespace Minio.Admin;

public class MinioAdminClient : IMinioAdminClient
{
    private readonly IMinioClient client;

    public MinioAdminClient(IMinioClient client)
    {
        this.client = client;
    }

    private string AdminApiVersion
    {
        get
        {
            var e = Environment.GetEnvironmentVariable("MADMIN_API_VERSION");
            if (e != null && e.Equals("v3")) return "v3";
            return "v4";
        }
    }

    #region [path prefix]

    private readonly string adminApiOldVersion = "v3";
    private string AdminApiPrefix => "admin/" + AdminApiVersion;
    private string AdminApiOldPrefix => "admin/" + adminApiOldVersion;

    private readonly string kmsApiVersion = "v1";
    private string KmsApiPrefix => "/" + kmsApiVersion;

    #endregion [path prefix]

    private AdminErrorResponse? GetErrorResponse(ResponseResult rr)
    {
        var d = JsonSerializer.Deserialize<AdminErrorResponse>(rr.Content);
        return d;
    }

    private async Task<ResponseResult?> SendRequestAsync(HttpMethod method, string path, AdminRequestArgs args, CancellationToken token)
    {
        var req = await client.CreateRequest(method, "minio", path).ConfigureAwait(false);
        req = args.BuildWithClient(req,client);
        var resp = await client.ExecuteTaskAsync(client.ResponseErrorHandlers, req, cancellationToken: token).ConfigureAwait(false);
        if (!resp.Response.IsSuccessStatusCode)
        {
            var err = GetErrorResponse(resp);
            if (err != null
                && string.Equals(err.Code, "XMinioAdminVersionMismatch", StringComparison.OrdinalIgnoreCase)
                && err.Message.Contains("downgrade", StringComparison.OrdinalIgnoreCase))
            {
                var pathV3 = ReplaceFirst(path, AdminApiVersion, adminApiOldVersion);
                req = await client.CreateRequest(method, "minio", pathV3).ConfigureAwait(false);
                req = args.BuildWithClient(req, client);
                return await client.ExecuteTaskAsync(client.ResponseErrorHandlers, req, cancellationToken: token).ConfigureAwait(false);
            }
        }

        return null;
    }
    public Task<AdminListServiceAccountsResult?> ListServiceAccountsAsync(CancellationToken token = default)
    {
        return ListServiceAccountsAsync(new AdminListServiceAccountsArgs(), token);
    }

    public async Task<AdminListServiceAccountsResult?> ListServiceAccountsAsync(AdminListServiceAccountsArgs args, CancellationToken token = default)
    {
        var path = $"{AdminApiPrefix}/list-service-accounts";
        using var resp = await SendRequestAsync(HttpMethod.Get, path, args, token).ConfigureAwait(false);
        if (resp == null) return null;
        if (resp.Response.IsSuccessStatusCode)
        {
            var dec = CryptoHelper.Decrypt(client.Config.SecretKey, resp.ContentStream);
            return JsonSerializer.Deserialize<AdminListServiceAccountsResult>(dec);
        }

        return null;
    }

    public async Task<AdminAddServiceAccountResult?> AddServiceAccountAsync(AdminAddServiceAccountArgs args, CancellationToken token = default)
    {
        var path = $"{AdminApiPrefix}/add-service-account";
        using var resp = await SendRequestAsync(HttpMethod.Put, path, args, token).ConfigureAwait(false);
        if (resp == null) return null;
        if (resp.Response.IsSuccessStatusCode)
        {
            var dec = CryptoHelper.Decrypt(client.Config.SecretKey, resp.ContentStream);
            return JsonSerializer.Deserialize<AdminAddServiceAccountResult>(dec);
        }

        return null;
    }

    /// <summary>
    /// 在字符串 str 中查找第一次完全匹配的子字符串 s，并将其替换为 k。
    /// </summary>
    /// <param name="str">原始字符串。</param>
    /// <param name="s">要查找的子字符串。</param>
    /// <param name="k">用于替换的字符串。</param>
    /// <returns>替换后的新字符串。</returns>
    private static string ReplaceFirst(string str, string s, string k)
    {
        if (s.Equals(k)) return str;
        if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(s))
        {
            // 如果 str 或 s 为空，防止异常，直接返回原字符串
            return str;
        }

        var index = str.IndexOf(s, StringComparison.Ordinal);  // 查找 s 在 str 中的第一次出现位置
        if (index != -1)  // 如果找到了匹配
        {
            // 构建新字符串：str 的前部分 + k + str 的剩余部分
            return str[..index] + k + str[(index + s.Length)..];
        }

        return str;  // 如果没有找到匹配，返回原字符串
    }

    public async Task<AdminListUsersResult?> ListUsersAsync(CancellationToken token = default)
    {
        var path = $"{AdminApiPrefix}/list-users";
        using var resp = await SendRequestAsync(HttpMethod.Get, path,  AdminRequestArgs.Empty, token).ConfigureAwait(false);
        if (resp == null) return null;
        if (resp.Response.IsSuccessStatusCode)
        {
            var dec = CryptoHelper.Decrypt(client.Config.SecretKey, resp.ContentStream);
            return JsonSerializer.Deserialize<AdminListUsersResult>(dec);
        }
        return null;
    }

    public async Task SetUserAsync(string accessKey, string secretKey,bool isEnabled=true, CancellationToken token = default)
    {
        var path = $"{AdminApiPrefix}/add-user";
        var args = new AdminRequestArgs();
        args.AddBodyParam("secretKey", secretKey);
        args.AddBodyParam("status", isEnabled? "enabled": "disabled");
        args.AddQueryParam("accessKey", accessKey);

        using var resp = await SendRequestAsync(HttpMethod.Put, path, args, token).ConfigureAwait(false);
        if(resp==null)
        {
            throw new Exception("[AdminApi] add-user failed");
        }
        if (!resp.Response.IsSuccessStatusCode)
        {
            throw new Exception(resp.Content);
        }
    }


}
