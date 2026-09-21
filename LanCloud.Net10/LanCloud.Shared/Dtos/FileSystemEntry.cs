namespace LanCloud.Shared.Dtos;

public record FileSystemEntry(
    string Name,
    string Path,
    bool IsDirectory,
    long Size,
    DateTime Created,
    DateTime LastModified);