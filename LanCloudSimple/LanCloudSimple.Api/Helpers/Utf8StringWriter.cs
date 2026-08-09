using System.Text;

namespace LanCloudSimple.Api.Helpers;

/// <summary>
/// A StringWriter that reports its encoding as UTF-8,
/// so XDocument.Save produces a proper UTF-8 XML declaration.
/// </summary>
public class Utf8StringWriter : StringWriter
{
    public override Encoding Encoding => Encoding.UTF8;
}
