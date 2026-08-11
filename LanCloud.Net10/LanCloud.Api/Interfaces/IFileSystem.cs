using LanCloud.Api.Models;

namespace LanCloud.Api.Interfaces;

public interface IFileSystem
{
    IAsyncEnumerable<FileSystemEntry> ListDirectory(
        string path,
        CancellationToken ct = default);

    Task<FileSystemEntry?> Get(
        string path,
        CancellationToken ct = default);

    Task<Stream?> OpenRead(
        string path,
        CancellationToken ct = default);

    Task Write(
        string path,
        Stream content,
        CancellationToken ct = default);

    Task Delete(
        string path,
        CancellationToken ct = default);

    Task CreateDirectory(
        string path,
        CancellationToken ct = default);
}
