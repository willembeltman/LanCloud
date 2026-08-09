namespace LanCloudSimple.Api.Models;

public class BrowseNode
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public DateTime? MediaDate { get; set; }
    public string? ClientId { get; set; }
}
