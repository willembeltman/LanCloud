namespace LanCloud.Interfaces;

public interface IDriveFileSystemEntry
{
    FileAttributes Attributes { get; }
    DateTime? CreationTime { get; }
    bool IsDirectory { get; }
    DateTime? LastAccessTime { get; }
    DateTime? LastWriteTime { get; }
    long? Length { get; }
    string Name { get; }
    string Path { get; }
}