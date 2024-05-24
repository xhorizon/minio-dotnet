using System.Net;
using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.DataModel.Args;


namespace Minio.Tests;

[TestClass]
public class MultipartUploadTest
{
    private const string ENDPOINT = "192.168.1.141:9000";
    private const string USERNAME = "minioadmin";
    private const string PASSWORD = "minioadmin";
    
    [TestMethod]
    public async Task CreateMultipartUpload()
    {
        using var hc = new HttpClient();
        var client = new MinioClient()
            .WithEndpoint(ENDPOINT).WithSSL(false)
            .WithCredentials(USERNAME,
                PASSWORD).WithHttpClient(hc)
            .Build();
        // var buks =await client.ListBucketsAsync();
        var bucket = "test";
        var objectName = "F5123BBC-53E9-4C37-8A55-507767906890";
        var args = new CreateMultipartUploadArgs
        {
            BucketName = bucket,
            ObjectName = objectName,
            ContentType = "application/octet-stream"
        };
      
        var resp = await client.CreateMultipartUploadAsync(args);
        Assert.IsTrue(!string.IsNullOrEmpty(resp.UploadId));
        var sss = (new SignObjectPartArgs())
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithUploadId(resp.UploadId)
            .WithPartNumber(1); 
     
     
        var aa = await client.SignMultipartUploadPartAsync(sss);
        await client.AbortMultipartUploadAsync(bucket, objectName, resp.UploadId);
        var li = (new ListIncompleteUploadsArgs { BucketName = bucket }).WithPrefix(objectName);
     
        var ups = client.ListIncompleteUploads(li);
        _= ups.Subscribe(cc => Assert.IsTrue(!string.Equals(resp.UploadId, cc.UploadId, StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task FullProgress()
    {
        using var hc = new HttpClient();
        var client = new MinioClient()
            .WithEndpoint(ENDPOINT).WithSSL(false)
            .WithCredentials(USERNAME,PASSWORD).WithHttpClient(hc)
            .Build();

        // var buks =await client.ListBucketsAsync();
        var bucket = "test";
        var objectName = "F5123BBC-53E9-4C37-8A55-507767906890.exe";
        var file = @"D:\fff.exe";

        Assert.IsTrue(File.Exists(file));
        /*
         * CreateMultipartUploadA
         */
        var args = new CreateMultipartUploadArgs
        {
            BucketName = bucket,
            ObjectName = objectName,
            ContentType = "application/octet-stream"
        };
        var resp = await client.CreateMultipartUploadAsync(args);

        Assert.IsTrue(resp.ResponseStatusCode == HttpStatusCode.OK);

        var uploadId = resp.UploadId;
        var eTagList = new Dictionary<int, string>();
        const int chunkSize = 8 * 1024 * 1024;
        var buffer = new byte[chunkSize];
        // var totalBytesRead = 0;
        var pn = 0;
        using (var fs = File.OpenRead(file))
        {
         
            var bytesRead = 0;
            while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                pn++;

                var putBuf = new byte[bytesRead];
                Buffer.BlockCopy(buffer, 0, putBuf, 0, bytesRead);

                var a2 = new SignObjectPartArgs()
                    .WithBucket(bucket)
                    .WithObject(objectName)
                    .WithUploadId(uploadId)
                    .WithPartNumber(pn)
                    .WithContentType("application/octet-stream");

                var s256 = GetSha256Str(putBuf);
                _ = a2.WithContentSha256(s256);

                var sup = await client.SignMultipartUploadPartAsync(a2);

                var sc = new ByteArrayContent(putBuf);
                sc.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                var reqMsg = new HttpRequestMessage(HttpMethod.Put, sup.RequestUri);
                reqMsg.Content = sc;

                foreach (var hh in sup.Headers)
                {
                    //if (hh.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue;
                    _= reqMsg.Headers.TryAddWithoutValidation(hh.Key, hh.Value);
                }

                var pRes = await hc.SendAsync(reqMsg);
                var b = await pRes.Content.ReadAsStringAsync().ConfigureAwait(false);
                Assert.IsTrue(pRes.IsSuccessStatusCode);
                var h2 = pRes.Headers.TryGetValues("ETag", out var etag);
                Assert.IsTrue(h2);
              
                eTagList.Add(pn, etag.First());
            }


            var a3 = new FinishMultipartUploadArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithUploadId(resp.UploadId)
                .WithETags(eTagList);

            var rrr = await client.FinishMultipartUploadAsync(a3).ConfigureAwait(false);
            Assert.IsTrue(rrr.StatusCode == HttpStatusCode.OK);
            var compETag = rrr.ETag.Trim('"');
            var sb = new StringBuilder();
            foreach (var et in eTagList.OrderBy(c => c.Key))
            {
                _=sb.Append(et.Value.Trim('"'));
            }

            var kTag = sb.ToString();

            using (var sh = MD5.Create())
            {
                var hb = sh.ComputeHash(HexStringToBytes(kTag));
                var ch = BitConverter.ToString(hb).Replace("-", "").ToLowerInvariant();
                ch = $"{ch}-{eTagList.Count}";
                Assert.IsTrue(compETag.Equals(ch, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    /// <summary>
    /// 16进制字符串转byte数组
    /// </summary>
    /// <param name="hexString">16进制字符</param>
    /// <returns></returns>
    private byte[] HexStringToBytes(string hexString)
    {
        // 将16进制秘钥转成字节数组
        var bytes = new byte[hexString.Length / 2];
        for (var x = 0; x < bytes.Length; x++)
        {
            var i = Convert.ToInt32(hexString.Substring(x * 2, 2), 16);
            bytes[x] = (byte)i;
        }

        return bytes;
    }

    private string GetSha256Str(byte[] buf)
    {
        using (var sh = SHA256.Create())
        {
            var hb = sh.ComputeHash(buf);
            return BitConverter.ToString(hb).Replace("-", "").ToLowerInvariant();
        }
    }


    [TestMethod]
    public async Task MMM()
    {
        var client = new MinioClient()
            .WithEndpoint(ENDPOINT).WithSSL(false)
            .WithCredentials(USERNAME,PASSWORD)
            .Build();
        // var buks =await client.ListBucketsAsync();
        var bucket = "test";
        var objectName = "D9A7A399-3D9C-4021-B79B-136D15B586DD";
        var f = @"E:\Download\rustup-init.exe";
        var arg = new PutObjectArgs();
        _=arg.WithBucket(bucket).WithObject(objectName).WithFileName(f);
        _=await client.PutObjectAsync(arg);
    }
}
