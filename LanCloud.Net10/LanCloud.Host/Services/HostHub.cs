using gAPI.Core.Client;
using gAPI.Core.Client.Interfaces;
using gAPI.Generated;
using LanCloud.Host.Models;
using LanCloud.Shared.Dtos;
using LanCloud.Shared.Interfaces;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;

namespace LanCloud.Host.Services;

public class HostHub(
    IClientConnection clientConnection,
    HostConfig config)
    : IHostedService
    , IHostHub
{
    async Task IHostedService.StartAsync(CancellationToken ct) 
        => await clientConnection.SubscribeAsync(this, ct);
    async Task IHostedService.StopAsync(CancellationToken ct)
        => await clientConnection.UnsubscribeAsync(this, ct);

    async IAsyncEnumerable<ShareEntryDto> IHostHub.ListDirectory(
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

    async IAsyncEnumerable<ShareEntryDto> IHostHub.Get(
        string relativeFullName,
        [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var share in config.Shares)
        {
            var entries = share.Get(relativeFullName, clientConnection.SessionId, ct);
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
    async IAsyncEnumerable<FileChunkDto> IHostHub.ReadFile(
        string relativeFullName,
        [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var share in config.Shares)
        {
            var chunks = share.ReadFile(
                relativeFullName,
                ct);

            await foreach (var chunk in chunks)
            {
                ct.ThrowIfCancellationRequested();

                yield return chunk;
            }

            if (ct.IsCancellationRequested)
                yield break;
        }
    }
}