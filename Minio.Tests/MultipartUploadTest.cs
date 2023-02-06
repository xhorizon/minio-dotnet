using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.DataModel;

namespace Minio.Tests;

[TestClass]
public class MultipartUploadTest
{
    [TestMethod]
    public async Task CreateMultipartUpload()
    {
        var client = new MinioClient()
            .WithEndpoint("192.168.100.200:9000").WithSSL(false)
            .WithCredentials("1Fx0JFU1TK3pHigQ",
                "8JPG1rDIqXGZJOW9rhNQ5CCwjHJNfwWJ")
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
        var sss = new SignObjectPartArgs();
        sss.WithBucket(bucket);
        sss.WithObject(objectName);
        sss.WithUploadId(resp.UploadId);
        sss.WithPartNumber(1);
        var aa= await client.SignMultipartUploadPart(sss);
        await client.AbortMultipartUploadAsync(bucket, objectName, resp.UploadId);
        var li = new ListIncompleteUploadsArgs { BucketName = bucket };
        li.WithPrefix(objectName);
        var ups = client.ListIncompleteUploads(li);
        ups.Subscribe(cc =>
        {
            Assert.IsTrue(!string.Equals(resp.UploadId, cc.UploadId, StringComparison.OrdinalIgnoreCase));
        });
    }
}