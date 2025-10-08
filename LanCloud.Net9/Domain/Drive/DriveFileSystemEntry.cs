using LanCloud.Interfaces;

namespace LanCloud.Domain.Drive;

public sealed class DriveFileSystemEntry
{
    public DriveFileSystemEntry(
        string path,
        string name,
        bool isDirectory,
        long? length,
        DateTime? creationTime,
        DateTime? lastAccessTime,
        DateTime? lastWriteTime,
        FileAttributes attributes)
    {
        Path = path;
        Name = name;
        IsDirectory = isDirectory;
        Length = length;
        CreationTime = creationTime;
        LastAccessTime = lastAccessTime;
        LastWriteTime = lastWriteTime;
        Attributes = attributes;
    }

    public string Path { get; }
    public string Name { get; }
    public bool IsDirectory { get; }
    public long? Length { get; }
    public DateTime? CreationTime { get; }
    public DateTime? LastAccessTime { get; }
    public DateTime? LastWriteTime { get; }
    public FileAttributes Attributes { get; }
}
