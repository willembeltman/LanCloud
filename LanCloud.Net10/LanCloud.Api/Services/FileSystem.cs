using gAPI.Generated;
using LanCloud.Api.Helpers;
using LanCloud.Api.Interfaces;
using LanCloud.Api.Models;
using LanCloud.Shared.Models;
using System.Runtime.CompilerServices;

namespace LanCloud.Api.Services;

public class FileSystem(
    IClientContext clientContext,
    RemovedItemsCollection removedEntities,
    ApiConfig apiConfig)
    : IFileSystem
{
    private readonly Dictionary<string, Entry> RespondedEntries = [];

    public LocalShare LocalShare =>
        apiConfig.LocalShare
        ?? new LocalShare(Path.Combine(Environment.CurrentDirectory, "LocalFiles"));

    public async Task CreateDirectory(string path, CancellationToken ct = default)
    {
        await LocalShare.CreateDirectory(path, ct);
        await removedEntities.CreateDirectory(path, ct);
    }

    public async Task Delete(string path, CancellationToken ct = default)
    {
        await LocalShare.Delete(path, ct);
        await removedEntities.Delete(path, ct);
    }

    public async Task<FileSystemEntry?> Get(string path, CancellationToken ct = default)
    {
        if (await removedEntities.IsRemoved(path, ct))
            return null;

        var allShareFiles = await clientContext.HostHub.ToAll.Get(path, ct).ToListAsync();
        var localShareFiles = LocalShare.Get(path, ct);
        await foreach (var localFile in localShareFiles)
            allShareFiles.Add(localFile);

        var shareFile = allShareFiles
            .OrderByDescending(a => a.GetLastDate())
            .FirstOrDefault();
        if (shareFile == null) return null;

        var fsFile = new FileSystemEntry(
            shareFile.Name,
            shareFile.Path,
            shareFile.IsDirectory,
            shareFile.Size,
            shareFile.Created,
            shareFile.LastModified);
        var entry = new Entry(fsFile, shareFile);
        RespondedEntries[path] = entry;

        return fsFile;
    }
    public async IAsyncEnumerable<FileSystemEntry> ListDirectory(
        string path,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var allShareFiles = await clientContext.HostHub.ToAll.ListDirectory(path, ct).ToListAsync();
        var localShareFiles = LocalShare.ListDirectory(path, ct);
        await foreach (var localFile in localShareFiles)
            allShareFiles.Add(localFile);

        var files = allShareFiles
            .GroupBy(a => a.Path)
            .Select(a => a.OrderByDescending(b => b.GetLastDate()).First());

        foreach (var shareFile in files)
        {
            if (await removedEntities.IsRemoved(shareFile.Path, ct))
                continue;

            var fsFile = new FileSystemEntry(
                shareFile.Name,
                shareFile.Path,
                shareFile.IsDirectory,
                shareFile.Size,
                shareFile.Created,
                shareFile.LastModified);
            var entry = new Entry(fsFile, shareFile);
            RespondedEntries[path] = entry;
            yield return fsFile;
        }
    }

    public async Task<Stream?> OpenRead(string path, CancellationToken ct = default)
    {
        if (await removedEntities.IsRemoved(path, ct))
            return null;

        if (!RespondedEntries.TryGetValue(path, out var respondedEntity))
            return null;

        var fileChunks = clientContext.HostHub
            .ToSession(respondedEntity.ShareEntryDto.SessionId)
            .ReadFile(path, ct);

        return new ChunkedStream(fileChunks, respondedEntity, ct);
    }

    public async Task Write(string path, Stream stream, CancellationToken ct = default)
    {
        await LocalShare.Write(path, stream, ct);
        await removedEntities.Write(path, ct);
    }
}