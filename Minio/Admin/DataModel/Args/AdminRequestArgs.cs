using System.Text;
using System.Text.Json;
using Minio.DataModel.Args;

namespace Minio.Admin.DataModel.Args;
public class AdminRequestArgs : RequestArgs
{
    protected IDictionary<string, string> QueryParams = new Dictionary<string,string>(StringComparer.Ordinal);
    protected IDictionary<string, string> BodyParams = new Dictionary<string,string>(StringComparer.Ordinal);
    public void WithQueryParams(IDictionary<string, string> @params)
    {
        foreach (var item in @params)
        {
            QueryParams.Add(item.Key, item.Value);
        }
    }

    public void AddQueryParam(string key,string value)
    {
        QueryParams[key] = value;
    }

    public void AddBodyParam(string key,string value)
    {
        BodyParams[key] = value;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        var builder = base.BuildRequest(requestMessageBuilder);

        if (QueryParams.Count > 0)
        {
            foreach (var kv in QueryParams)
            {
                builder.AddQueryParameter(kv.Key, kv.Value);
            }
        }
        return builder;
    }

    internal  HttpRequestMessageBuilder BuildWithClient(HttpRequestMessageBuilder requestMessageBuilder, IMinioClient client)
    {
        var builder= base.BuildRequest(requestMessageBuilder);

        if (QueryParams.Count > 0)
        {
            foreach (var kv in QueryParams) { 
             builder.AddQueryParameter(kv.Key, kv.Value);
            }
        }

        if (BodyParams.Count > 0)
        {
            var jb = JsonSerializer.Serialize(BodyParams);
            var ss = CryptoHelper.Encrypt(client.Config.SecretKey, Encoding.UTF8.GetBytes(jb));
            builder.SetBody(ss);
        }
       
        return builder;
    }

    public static AdminRequestArgs Empty => new ();
}
