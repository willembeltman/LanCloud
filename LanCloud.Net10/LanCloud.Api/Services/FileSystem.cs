using LanCloud.Api.Interfaces;
using LanCloud.Api.Models;

namespace LanCloud.Api.Services;

public class FileSystem : IFileSystem
{
    public Task CreateDirectory(string path, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task Delete(string path, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<FileSystemEntry?> Get(string path, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileSystemEntry> ListDirectory(string path, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Stream?> OpenRead(string path, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task Write(string path, Stream content, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}