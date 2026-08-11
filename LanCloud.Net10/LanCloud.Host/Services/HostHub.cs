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
        string relativeName,
        [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var share in config.Shares)
        {
            var list = share.ListDirectory(relativeName, ct);
            await foreach (var file in list)
            {
                yield return file;
                if (ct.IsCancellationRequested)
                    yield break;
            }
            if (ct.IsCancellationRequested)
                yield break;
        }
    }

    IAsyncEnumerable<ShareEntryDto> IHostHub.Get(
        string relativeFullName,
        [EnumeratorCancellation] CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}