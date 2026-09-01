using gAPI.Core.Dtos;
using gAPI.Generated;
using LanCloud.Host.Models;
using LanCloud.Shared.Dtos;
using LanCloud.Shared.Interfaces;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LanCloud.Host.Services;

public class HostHub(
    IClientConnection clientConnection,
    ITestApi testApi,
    IAuthenticatedHttpClient authenticatedHttpClient,
    HostConfig config)
    : IHostedService
    , IHostHub
{
    async Task IHostedService.StartAsync(CancellationToken ct)
    {
        await clientConnection.SubscribeAsync(this, ct);
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

    public async Task Test1()
    {
    }
    public async IAsyncEnumerable<string> Test3([EnumeratorCancellation]CancellationToken ct)
    {
        yield return "1";
        await Task.Yield();
        yield return "2";
        await Task.Yield();
        yield return "3";
        await Task.Yield();
    }

    public async Task Test4(string name, IAsyncEnumerable<string> test, IAsyncEnumerable<string> test2)
    {
        await foreach (var testItem in test)
        {
            //await Task.Delay(40000);
        }
        await foreach (var testItem in test2)
        {
        }
    }
    public async IAsyncEnumerable<string> Test6(string name, IAsyncEnumerable<string> test, IAsyncEnumerable<string> test2, [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var testItem in test)
        {
            //await Task.Delay(40000);
        }
        await foreach (var testItem in test2)
        {
        }

        yield return "1";
        //await Task.Yield();
        yield return "2";
        //await Task.Yield();
        yield return "3";
        //await Task.Yield();
    }

    public async Task StartTest(CancellationToken ct)
    {
        var state = await authenticatedHttpClient.GetStateAsync();
        state.Index++;

        // Dit draait op de client
        async IAsyncEnumerable<string> test()
        {
            yield return "1";
            yield return "2";
            yield return "3";
        }

        var tries = 10;
        Stopwatch watch = Stopwatch.StartNew();
        for (int i = 0; i < tries; i++)
        {
            // Call naar server
            var list = testApi.Test6(
                "test", 
                test(), 
                test(), 
                ct);

            await foreach (var item in list)
            {

            }
        }
        var avg = watch.ElapsedMilliseconds / 
            tries;
        Console.WriteLine(avg);
    }
}