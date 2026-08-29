using gAPI.Core.Dtos;
using gAPI.Generated;
using LanCloud.Host.Models;
using LanCloud.Shared.Dtos;
using LanCloud.Shared.Interfaces;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;

namespace LanCloud.Host.Services;

public class HostHub(
    IClientConnection clientConnection,
    IHostApi hostApi,
    HostConfig config)
    : IHostedService
    , IHostHub
{
    async Task IHostedService.StartAsync(CancellationToken ct)
    {
        await clientConnection.SubscribeAsync(this, ct);

        //async IAsyncEnumerable<string> test()
        //{
        //    yield return "1";
        //    await Task.Yield();
        //    yield return "2";
        //    await Task.Yield();
        //    yield return "3";
        //    await Task.Yield();
        //}

        //await hostApi.Test("test", test(), test());
    }

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

    public async Task Test(string name, IAsyncEnumerable<string> test, IAsyncEnumerable<string> test2)
    {
        await foreach (var testItem in test)
        {
        }
        await foreach (var testItem in test2)
        {
        }
    }
}