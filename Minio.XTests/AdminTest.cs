using System.Text.Json;
using Minio.Admin;
using Minio.Admin.DataModel.Args;
using Xunit.Abstractions;

namespace Minio.XTests;
public class AdminTest
{
    private readonly ITestOutputHelper output;

    public AdminTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    private const string ENDPOINT = "192.168.1.221:9000";
    private const string ACCESSKEY = "admin";
    private const string SECRETKEY = "admin123456";

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

    [Fact]
    public async Task CreateServiceAccount()
    {
        var c = GetClient();
        var exp = DateTimeOffset.UtcNow.AddDays(2);
        var args = new AdminAddServiceAccountArgs()
            .WithName("nn1")
            .WithDescription("dd1")
            .WithExpiration(exp);
            
        var rr = await c.AddServiceAccountAsync(args, CancellationToken.None);

        Assert.NotNull(rr);
        Assert.NotNull(rr.Credentials);

        output.WriteLine(JsonSerializer.Serialize(rr));
    }
 
}
