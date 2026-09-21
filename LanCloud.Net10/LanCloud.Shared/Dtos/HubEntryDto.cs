using gAPI.Core.Ids;

namespace LanCloud.Shared.Dtos;

public record HubEntryDto(
    string Name,
    string Path,
    bool IsDirectory,
    long Size,
    DateTime Created,
    DateTime LastModified,
    SessionId? SessionId) 
    : FileSystemEntry(Name, Path, IsDirectory, Size, Created, LastModified)
{
    public DateTime GetLastDate()
    {
        if (Created > LastModified) return Created;
        return LastModified;
    }
}
