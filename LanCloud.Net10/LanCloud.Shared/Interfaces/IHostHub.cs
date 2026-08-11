using gAPI.Core.Attributes;
using LanCloud.Shared.Dtos;

namespace LanCloud.Shared.Interfaces;

[GenerateHub]
public interface IHostHub
{
    IAsyncEnumerable<ShareEntryDto> ListDirectory(string relativePath, CancellationToken ct);
    IAsyncEnumerable<ShareEntryDto> Get(string relativeFullName, CancellationToken ct);
    IAsyncEnumerable<FileChunkDto> ReadFile(string relativeFullName, CancellationToken ct);
}
