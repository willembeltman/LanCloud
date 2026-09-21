using gAPI.Core.Dtos;
using LanCloud.Api.Models;
using LanCloud.Shared.Dtos;

namespace LanCloud.Api.Interfaces;

public interface IFileSystemDirect
{
    Task Append(string path, Stream stream, CancellationToken ct);
    Task<AuthStateUserDto?> AuthenticateUser(string? userName, string? password, CancellationToken ct);
    Task CreateDirectory(string path, CancellationToken ct);
    Task Delete(string path, CancellationToken ct);
    Task<FileSystemEntry?> Get(string path, CancellationToken ct);
    Task<AuthenticationInfo> GetAuthenticationInfo(CancellationToken ct);
    IAsyncEnumerable<FileSystemEntry> ListDirectory(string path, CancellationToken ct);
    Task Move(string sourcePath, string destinationPath, CancellationToken ct);
    Task<Stream?> OpenRead(string path, CancellationToken ct);
    Task Write(string path, Stream stream, CancellationToken ct);
}