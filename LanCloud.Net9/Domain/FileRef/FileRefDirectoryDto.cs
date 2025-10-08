namespace LanCloud.Domain.FileRef;

public class FileRefDirectoryDto
{
    public FileRefDirectoryDto() { }
    public FileRefDirectoryDto(IFileRefDirectory fileRefDirectory)
    {
        Path = fileRefDirectory.Path;
        Exists = fileRefDirectory.Exists;
        LastWriteTime = fileRefDirectory.LastWriteTime;
    }

    public string Path { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public DateTime LastWriteTime { get; set; }
}
