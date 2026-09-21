using gAPI.Core.Attributes;
using gAPI.Core.Dtos;
using LanCloud.Api.Models;
using LanCloud.Shared.Dtos;

namespace LanCloud.Shared.Interfaces;

[GenerateApi]
public interface IFileSystemApi
{
    Task<AuthStateUserDto?> AuthenticateUser(string? userName, string? password, CancellationToken ct);
    Task CreateDirectory(string path, CancellationToken ct);
    Task Delete(string path, CancellationToken ct);
    Task<FileSystemEntry?> Get(string path, CancellationToken ct);
    Task<AuthenticationInfo> GetAuthenticationInfo(CancellationToken ct);
    IAsyncEnumerable<FileSystemEntry> ListDirectory(string path, CancellationToken ct);
    Task Move(string sourcePath, string destinationPath, CancellationToken ct);

    IAsyncEnumerable<byte[]> OpenRead(string path, long startOffset, CancellationToken ct);
    Task Write(string path, long startOffset, IAsyncEnumerable<byte[]> stream, CancellationToken ct);
    Task Append(string path, IAsyncEnumerable<byte[]> stream, CancellationToken ct);
}