namespace LanCloudSimple.Shared.Models;

public class CloudFileDto
{
    public string Path { get; set; } = string.Empty; // e.g. "Share1/Folder1/image.jpg"
    public long Size { get; set; }
    public DateTime LastWriteTime { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime MediaDate { get; set; }
}
