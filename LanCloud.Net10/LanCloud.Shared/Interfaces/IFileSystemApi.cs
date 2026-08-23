using LanCloud.Shared.Dtos;

namespace LanCloud.Shared.Interfaces;

public interface IFileSystemApi
{
    Task<bool> Authenticate(string username, string password, CancellationToken ct);
    Task CreateDirectory(string path, CancellationToken ct = default);
    Task Delete(string path, CancellationToken ct = default);
    Task<FileSystemEntry?> Get(string path, CancellationToken ct = default);
    Task<AuthenticationInfo> GetAuthenticationInfo(CancellationToken ct);
    IAsyncEnumerable<FileSystemEntry> ListDirectory(string path, CancellationToken ct = default);
    Task Move(string sourcePath, string destinationPath, CancellationToken ct = default);
    Task<Stream?> OpenRead(string path, CancellationToken ct = default);
    Task Write(string path, Stream stream, CancellationToken ct = default);
}