using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Minio.DataModel.Result;

[Serializable]
[XmlRoot(ElementName = "CompleteMultipartUploadResult", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
public class FinishMultipartUploadResult
{
    public string Bucket { get; set; }
    public string ETag { get; set; }
    public string Key { get; set; }
}
