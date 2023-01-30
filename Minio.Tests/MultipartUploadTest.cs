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
            .WithEndpoint("play.min.io").WithSSL(true)
            .WithCredentials("Q3AM3UQ867SPQQA43P2F",
                "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG")
            .Build();
        // var buks =await client.ListBucketsAsync();
        var bucket = "buckettest";
        var objectName = "F5123BBC-53E9-4C37-8A55-507767906890";
        var args = new CreateMultipartUploadArgs
        {
            BucketName = bucket,
            ObjectName = objectName,
            ContentType = "application/octet-stream"
        };
        var resp = await client.CreateMultipartUploadAsync(args);
        Assert.IsTrue(!string.IsNullOrEmpty(resp.UploadId));
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