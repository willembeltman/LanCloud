using LanCloud.Interfaces;

namespace LanCloud.Models.Dtos;

public class FileRefDirectoryDto
{
    public FileRefDirectoryDto() { }
    public FileRefDirectoryDto(IFileDirectory fileRefDirectory)
    {
        Path = fileRefDirectory.Path;
        Exists = fileRefDirectory.Exists;
        LastWriteTime = fileRefDirectory.LastWriteTime;
    }

    public string Path { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public DateTime LastWriteTime { get; set; }
}
