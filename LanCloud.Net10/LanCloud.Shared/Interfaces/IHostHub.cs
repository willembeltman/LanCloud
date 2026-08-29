using gAPI.Core.Attributes;
using gAPI.Core.Dtos;
using LanCloud.Shared.Dtos;

namespace LanCloud.Shared.Interfaces;

[GenerateHub]
public interface IHostHub
{
    Task Test(string name, IAsyncEnumerable<string> test, IAsyncEnumerable<string> test2);
    IAsyncEnumerable<ShareEntryDto> ListDirectory(string relativePath, CancellationToken ct);
    IAsyncEnumerable<ShareEntryDto> Get(string relativeFullName, CancellationToken ct);
    IAsyncEnumerable<DataChunkDto> ReadFile(string relativeFullName, long startOffset, CancellationToken ct);
}
