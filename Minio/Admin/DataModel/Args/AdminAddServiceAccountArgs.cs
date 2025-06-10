#nullable enable
using System.Xml.Linq;

namespace Minio.Admin.DataModel.Args;
public class AdminAddServiceAccountArgs: AdminRequestArgs
{
    public AdminAddServiceAccountArgs WithPolicy(string policy)
    {
        AddBodyParam("policy", policy);
        return this;
    }

    public AdminAddServiceAccountArgs WithTargetUser(string targetUser)
    {
        AddBodyParam("targetUser", targetUser);
        return this;
    }

    public AdminAddServiceAccountArgs WithAccessKey(string ak)
    {
        AddBodyParam("accessKey", ak);
        return this;
    }

    public AdminAddServiceAccountArgs WithSecretKey(string sk)
    {
        AddBodyParam("secretKey", sk);
        return this;
    }

    public AdminAddServiceAccountArgs WithName(string name)
    {
        AddBodyParam("name", name);
        return this;
    }

    public AdminAddServiceAccountArgs WithDescription(string desc) {
        AddBodyParam("description", desc);
        return this;
    }

    public AdminAddServiceAccountArgs WithExpiration(DateTimeOffset dt)
    {
        var exp = dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", null);
        AddBodyParam("expiration", exp);
        return this;
    }
}
