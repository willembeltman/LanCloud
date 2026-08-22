using gAPI.Generated;
using LanCloud.Api.Helpers;
using LanCloud.Api.Interfaces;
using LanCloud.Api.Models;
using LanCloud.Shared.Dtos;
using LanCloud.Shared.Models;
using System.Runtime.CompilerServices;

namespace LanCloud.Api.Services;

public class FileSystem(
    IClientContext clientContext,
    EntryCollection entryCollection,
    ApiConfig apiConfig)
    : IFileSystem
{
    public LocalShare LocalShare =>
        apiConfig.LocalShare
        ?? new LocalShare(Path.Combine(Environment.CurrentDirectory, "LocalFiles"));

    public async Task CreateDirectory(string path, CancellationToken ct = default)
    {
        await LocalShare.CreateDirectory(path, ct);
        await entryCollection.CreateDirectory(path, ct);
    }

    public async Task Delete(string path, CancellationToken ct = default)
    {
        await LocalShare.Delete(path, ct);
        await entryCollection.Delete(path, ct);
    }

    public async Task Move(string sourcePath, string destinationPath, CancellationToken ct = default)
    {
        await LocalShare.Move(sourcePath, destinationPath, ct);
        await entryCollection.Delete(sourcePath, ct);
        await entryCollection.CreateDirectory(destinationPath, ct);
    }

    public async Task<FileSystemEntry?> Get(string path, CancellationToken ct = default)
    {
        if (entryCollection.IsRemoved(path))
            return null;

        if (string.IsNullOrEmpty(path))
        {
            var rootFsEntry = new FileSystemEntry("", "", true, 0, DateTime.UtcNow, DateTime.UtcNow);
            var rootEntry = new Entry(rootFsEntry, new ShareEntryDto { Name = "", Path = "", IsDirectory = true });
            await entryCollection.Responded("", rootEntry, ct);
            return rootFsEntry;
        }

        var allShareFiles = new List<ShareEntryDto>();
        try
        {
            var remoteFiles = await clientContext.HostHub.ToAll.Get(path, ct).ToListAsync(cancellationToken: ct);
            allShareFiles.AddRange(remoteFiles);
        }
        catch
        {
            // Negeren als er geen host clients verbonden zijn
        }

        var localShareFiles = LocalShare.Get(path, ct);
        await foreach (var localFile in localShareFiles)
            allShareFiles.Add(localFile);

        var shareFile = allShareFiles
            .OrderByDescending(a => a.GetLastDate())
            .FirstOrDefault();
        if (shareFile == null) return null;

        return await CreateFileSystemEntry(path, shareFile, ct);
    }
    public async IAsyncEnumerable<FileSystemEntry> ListDirectory(
        string path,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var allShareFiles = new List<ShareEntryDto>();
        try
        {
            var remoteFiles = await clientContext.HostHub.ToAll.ListDirectory(path, ct).ToListAsync(cancellationToken: ct);
            allShareFiles.AddRange(remoteFiles);
        }
        catch
        {
            // Negeren als er geen host clients verbonden zijn
        }

        var localShareFiles = LocalShare.ListDirectory(path, ct);
        await foreach (var localFile in localShareFiles)
            allShareFiles.Add(localFile);

        var files = allShareFiles
            .GroupBy(a => a.Path)
            .Select(a => a.OrderByDescending(b => b.GetLastDate()).First());

        foreach (var shareFile in files)
        {
            if (entryCollection.IsRemoved(shareFile.Path))
                continue;

            yield return await CreateFileSystemEntry(path, shareFile, ct);
        }
    }

    public async Task<Stream?> OpenRead(string path, CancellationToken ct = default)
    {
        if (entryCollection.IsRemoved(path))
            return null;

        if (!entryCollection.RespondedEntries.TryGetValue(path, out var respondedEntity))
        {
            var fetched = await Get(path, ct);
            if (fetched == null || !entryCollection.RespondedEntries.TryGetValue(path, out respondedEntity))
                return null;
        }

        IAsyncEnumerable<FileChunkDto> fileChunks;
        if (string.IsNullOrEmpty(respondedEntity.ShareEntryDto.SessionId.Value))
        {
            fileChunks = LocalShare.ReadFile(path, ct);
        }
        else
        {
            fileChunks = clientContext.HostHub
                .ToSession(respondedEntity.ShareEntryDto.SessionId)
                .ReadFile(path, ct);
        }

        return new ChunkedStream(fileChunks, respondedEntity, ct);
    }

    public async Task Write(string path, Stream stream, CancellationToken ct = default)
    {
        await LocalShare.Write(path, stream, ct);
        await entryCollection.Write(path, ct);
    }

    private async Task<FileSystemEntry> CreateFileSystemEntry(string path, ShareEntryDto shareFile, CancellationToken ct)
    {
        var fsFile = new FileSystemEntry(
            shareFile.Name,
            shareFile.Path,
            shareFile.IsDirectory,
            shareFile.Size,
            shareFile.Created,
            shareFile.LastModified);
        var entry = new Entry(fsFile, shareFile);
        await entryCollection.Responded(path, entry, ct);
        return fsFile;
    }
}