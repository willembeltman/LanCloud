using gAPI.Core.Dtos;
using gAPI.Generated;
using LanCloud.Shared.Dtos;
using LanCloud.Shared.Interfaces;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;

namespace LanCloud.Host;

public class HostHub(
    IClientConnection clientConnection,
    LanCloudHostConfig config)
    : IHostedService
    , IHostHub
{
    async Task IHostedService.StartAsync(CancellationToken ct) => await clientConnection.SubscribeAsync(this, ct);

    async Task IHostedService.StopAsync(CancellationToken ct)
        => await clientConnection.UnsubscribeAsync(this, ct);

    async IAsyncEnumerable<HubEntryDto> IHostHub.ListDirectory(
        string relativeFullName,
        [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var share in config.Shares)
        {
            var entries = share.ListDirectory(relativeFullName, clientConnection.SessionId, ct);
            await foreach (var entry in entries)
            {
                yield return entry;
                if (ct.IsCancellationRequested)
                    yield break;
            }
            if (ct.IsCancellationRequested)
                yield break;
        }
    }

    async IAsyncEnumerable<HubEntryDto> IHostHub.Get(
        string relativeFullName,
        [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var share in config.Shares)
        {
            var entries = share.Get(relativeFullName, clientConnection.SessionId, ct);
            await foreach (var entry in entries)
            {
                ct.ThrowIfCancellationRequested();
                yield return entry;
            }
        }
    }

    async IAsyncEnumerable<DataChunkDto> IHostHub.ReadFile(
        string relativeFullName,
        long startOffset,
        [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var share in config.Shares)
        {
            var chunks = share.ReadFile(
                relativeFullName,
                startOffset,
                ct);

            await foreach (var chunk in chunks)
            {
                ct.ThrowIfCancellationRequested();
                yield return chunk;
            }
        }
    }
}