using Minio.Admin;
using Xunit.Abstractions;

namespace Minio.XTests;
public class AdminTest
{
    private readonly ITestOutputHelper output;

    public AdminTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    private const string ENDPOINT = "192.168.100.200:9000";
    private const string ACCESSKEY = "H0hZ2gb27v0pzlP4tlzd";
    private const string SECRETKEY = "biFtE6rdAeud0TotDzeTiWHWz1a22YjWYoOSAmsd";

    private IMinioAdminClient GetClient()
    {
        var hc = new HttpClient();
        var client = new MinioClient()
            .WithEndpoint(ENDPOINT).WithSSL(false)
            .WithCredentials(ACCESSKEY,
                SECRETKEY).WithHttpClient(hc)
            .Build();
        return new MinioAdminClient(client);
    }


    [Fact]
    public async Task ListServiceAccounts()
    {
        var c = GetClient();
        var d = await c.ListServiceAccountsAsync(new Admin.DataModel.Args.AdminListServiceAccountsArgs());
        Assert.NotNull(d);
        Assert.True(d.Accounts.Count > 0);
    }

    [Fact]
    public async Task ListUsersAsync() {
        var c = GetClient();
        var d = await c.ListUsersAsync();
        Assert.NotNull(d);
        Assert.True(d.Count > 0);
    }

    [Fact]
    public  Task AddUser()
    {
        var c = GetClient();
        return c.SetUserAsync("0pzlP4tlzdH0hZ2gb27v", "TiWHWz1a22YjWYoOSAmsdbiFtE6rdAeud0TotDze");

    }
 
}
