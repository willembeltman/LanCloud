using gAPI.Core.Attributes;
using LanCloud.Shared.Dtos;

namespace LanCloud.Shared.Interfaces;

[GenerateHub]
public interface IHostHub
{
    IAsyncEnumerable<FileInfoDto> ListDirectory(string relativeName, CancellationToken ct);
}
