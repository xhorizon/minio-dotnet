using System.Text;
using System.Xml.Linq;

namespace Minio.DataModel.Args;
public class FinishMultipartUploadArgs : ObjectWriteArgs<FinishMultipartUploadArgs>
{
    public FinishMultipartUploadArgs()
    {
        RequestMethod = HttpMethod.Post;
    }

    public string UploadId { get; set; }
    public IDictionary<int, string> ETags { get; set; } 

    internal override void Validate()
    {
        base.Validate();
        if (string.IsNullOrWhiteSpace(UploadId))
            throw new InvalidOperationException(nameof(UploadId) + " cannot be empty.");
        if (ETags is null || ETags.Count <= 0)
            throw new InvalidOperationException(nameof(ETags) + " dictionary cannot be empty.");
    }

    public FinishMultipartUploadArgs WithUploadId(string uploadId)
    {
        UploadId = uploadId;
        return this;
    }

    public FinishMultipartUploadArgs WithETags(IDictionary<int, string> etags)
    {
        if (etags?.Count > 0) ETags = new Dictionary<int, string>(etags);
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("uploadId", $"{UploadId}");
        var parts = new List<XElement>();

        for (var i = 1; i <= ETags.Count; i++)
            parts.Add(new XElement("Part",
                new XElement("PartNumber", i),
                new XElement("ETag", ETags[i])));

        var completeMultipartUploadXml = new XElement("CompleteMultipartUpload", parts);
        var bodyString = completeMultipartUploadXml.ToString();
        ReadOnlyMemory<byte> bodyInBytes = Encoding.UTF8.GetBytes(bodyString);
        requestMessageBuilder.BodyParameters.Add("content-type", "application/xml");
        requestMessageBuilder.SetBody(bodyInBytes);
        // var bodyInCharArr = Encoding.UTF8.GetString(requestMessageBuilder.Content).ToCharArray();

        return requestMessageBuilder;
    }
}
