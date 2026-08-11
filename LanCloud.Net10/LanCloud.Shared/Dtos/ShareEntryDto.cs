namespace LanCloud.Shared.Dtos;

public class ShareEntryDto
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public DateTime Created { get; set; }
    public DateTime LastModified { get; set; }

    public DateTime GetLastDate()
    {
        if (Created > LastModified) return Created;
        return LastModified;
    }
}
