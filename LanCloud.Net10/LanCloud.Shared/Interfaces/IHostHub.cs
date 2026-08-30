using gAPI.Core.Attributes;
using gAPI.Core.Dtos;
using LanCloud.Shared.Dtos;

namespace LanCloud.Shared.Interfaces;

[GenerateHub]
public interface IHostHub
{
    IAsyncEnumerable<ShareEntryDto> ListDirectory(string relativePath, CancellationToken ct);
    IAsyncEnumerable<ShareEntryDto> Get(string relativeFullName, CancellationToken ct);
    IAsyncEnumerable<DataChunkDto> ReadFile(string relativeFullName, long startOffset, CancellationToken ct);
    Task Test1();
    IAsyncEnumerable<string> Test3(CancellationToken ct);
    Task Test4(string name, IAsyncEnumerable<string> test, IAsyncEnumerable<string> test2);
    IAsyncEnumerable<string> Test6(string name, IAsyncEnumerable<string> test, IAsyncEnumerable<string> test2, CancellationToken ct);
}
