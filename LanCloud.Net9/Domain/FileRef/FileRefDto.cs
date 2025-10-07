namespace LanCloud.Domain.FileRef;

public class FileRefDto
{
    public FileRefDto() { }
    public FileRefDto(IFileRef fileRef)
    {
        Path = fileRef.Path;
        Exists = fileRef.Exists;
        Hash = fileRef.Hash;
        Length = fileRef.Length;
        LastWriteTime = fileRef.LastWriteTime;
    }

    public string? Path { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public string? Hash { get; set; }
    public long? Length { get; set; }
    public DateTime LastWriteTime { get; set; }
}
