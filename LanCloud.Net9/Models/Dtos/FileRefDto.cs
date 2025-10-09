using LanCloud.Interfaces;

namespace LanCloud.Models.Dtos;

public class FileRefDto
{
    public FileRefDto() { }
    public FileRefDto(IFile fileRef)
    {
        Path = fileRef.Path;
        Exists = fileRef.Exists;
        Hash = fileRef.Hash;
        Length = fileRef.Length;
        LastWriteTime = fileRef.LastWriteTime;
    }

    public string Path { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public string Hash { get; set; } = string.Empty;
    public long Length { get; set; }
    public DateTime LastWriteTime { get; set; }
}
