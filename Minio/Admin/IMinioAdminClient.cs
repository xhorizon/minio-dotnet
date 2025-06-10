using Minio.Admin.DataModel.Args;

#nullable enable

using Minio.Admin.DataModel.Result;

namespace Minio.Admin;

public interface IMinioAdminClient
{
    /// <summary>
    /// list service accounts belonging to the specified user
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<AdminListServiceAccountsResult?> ListServiceAccountsAsync(CancellationToken token = default);

    /// <summary>
    /// list service accounts belonging to the specified user
    /// </summary>
    /// <param name="args"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<AdminListServiceAccountsResult?> ListServiceAccountsAsync(AdminListServiceAccountsArgs args, CancellationToken token = default);

    /// <summary>
    /// creates a new service account belonging to the user sending the request while restricting the service account permission by the given policy document.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<AdminAddServiceAccountResult?> AddServiceAccountAsync(AdminAddServiceAccountArgs args, CancellationToken token=default);
    
    /// <summary>
    /// list all users.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<AdminListUsersResult?> ListUsersAsync(CancellationToken token = default);

    /// <summary>
    /// update user secret key or account status.
    /// </summary>
    /// <param name="accessKey"></param>
    /// <param name="secretKey"></param>
    /// <param name="isEnabled"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task SetUserAsync(string accessKey, string secretKey, bool isEnabled = true, CancellationToken token = default);
}
