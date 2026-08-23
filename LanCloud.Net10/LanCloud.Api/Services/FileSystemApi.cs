using gAPI.Generated;
using LanCloud.Api.Helpers;
using LanCloud.Api.Models;
using LanCloud.Shared.Dtos;
using LanCloud.Shared.Interfaces;
using LanCloud.Shared.Models;
using System.Runtime.CompilerServices;
using System.Text;

namespace LanCloud.Api.Services;

// Edge cases:
// 1. Als je een bestand verwijderd wat zowel op localshare als remote staat

public class FileSystemApi(
    IClientContext clientContext,
    EntryCollection entryCollection,
    LocalShare localShare)
    : IFileSystemApi
{

    public async Task<AuthenticationInfo> GetAuthenticationInfo(CancellationToken ct)
    {
        return new AuthenticationInfo(
            Required: true,
            Realm: "LanCloud");
    }

    public async Task<bool> Authenticate(string username, string password, CancellationToken ct)
    {
        return true;
    }

    public async Task CreateDirectory(string path, CancellationToken ct = default)
    {
        await localShare.CreateDirectory(path, ct);
        await entryCollection.CreateDirectory(path, ct);
    }

    public async Task Delete(string path, CancellationToken ct = default)
    {
        try
        {
            await localShare.Delete(path, ct);
        }
        catch (FileNotFoundException)
        {
            // Remote-only entries bestaan lokaal niet; de tombstone hieronder verbergt ze in de API.
        }
        catch (DirectoryNotFoundException)
        {
            // Remote-only entries bestaan lokaal niet; de tombstone hieronder verbergt ze in de API.
        }

        await entryCollection.Delete(path, ct);
    }

    public async Task Move(string sourcePath, string destinationPath, CancellationToken ct = default)
    {
        var sourceExistsLocally = await LocalEntryExists(sourcePath, ct);

        await localShare.Move(sourcePath, destinationPath, ct);
        await entryCollection.Move(
            sourcePath,
            destinationPath,
            trackSourceAsMoved: !sourceExistsLocally,
            ct);
    }

    public async Task<FileSystemEntry?> Get(string path, CancellationToken ct = default)
    {
        path = EntryCollection.Normalize(path);

        if (entryCollection.IsRemoved(path))
            return null;

        if (string.IsNullOrEmpty(path))
        {
            var rootFsEntry = new FileSystemEntry("", "", true, 0, DateTime.UtcNow, DateTime.UtcNow);
            var rootEntry = new Entry(rootFsEntry, new ShareEntryDto { Name = "", Path = "", IsDirectory = true }, "");
            await entryCollection.Responded("", rootEntry, ct);
            return rootFsEntry;
        }

        var readPath = entryCollection.ResolveReadPath(path);
        var allShareFiles = await GetShareEntries(readPath, ct);

        var shareFile = allShareFiles
            .OrderByDescending(a => a.GetLastDate())
            .FirstOrDefault();
        if (shareFile == null) return null;

        return await CreateFileSystemEntry(path, readPath, shareFile, ct);
    }

    public async IAsyncEnumerable<FileSystemEntry> ListDirectory(string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        path = EntryCollection.Normalize(path);

        if (entryCollection.IsRemoved(path))
            yield break;

        var allShareFiles = new List<ShareEntryDto>();
        var readPath = entryCollection.ResolveReadPath(path);
        allShareFiles.AddRange(await ListShareEntries(readPath, ct));

        foreach (var movedSource in entryCollection.GetMovedSourcesForDirectory(path))
            allShareFiles.AddRange(await GetShareEntries(movedSource, ct));

        var files = allShareFiles
            .Select(a => (ShareFile: a, VisiblePath: entryCollection.ResolveVisiblePath(a.Path)))
            .Where(a => GetParentPath(a.VisiblePath) == path)
            .GroupBy(a => a.VisiblePath)
            .Select(a => a.OrderByDescending(b => b.ShareFile.GetLastDate()).First());

        foreach (var (shareFile, visiblePath) in files)
        {
            if (entryCollection.IsRemoved(visiblePath))
                continue;

            yield return await CreateFileSystemEntry(visiblePath, shareFile.Path, shareFile, ct);
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

        IAsyncEnumerable<FileChunkDto> OpenChunks(long startOffset, CancellationToken streamCt)
        {
            if (respondedEntity.ShareEntryDto.SessionId == null)
            {
                return localShare
                    .ReadFile(respondedEntity.ReadPath, startOffset, streamCt);
            }

            return clientContext.HostHub
                .ToSession(respondedEntity.ShareEntryDto.SessionId.Value)
                .ReadFile(respondedEntity.ReadPath, startOffset, streamCt);
        }

        return new ChunkedStream(OpenChunks, respondedEntity, ct);
    }

    public async Task Write(string path, Stream stream, CancellationToken ct = default)
    {
        await localShare.Write(path, stream, ct);
        await entryCollection.Write(path, ct);
    }

    private async Task<List<ShareEntryDto>> GetShareEntries(string path, CancellationToken ct)
    {
        var allShareFiles = new List<ShareEntryDto>();
        try
        {
            var remoteFiles = await clientContext.HostHub.ToAll
                .Get(path, ct)
                .ToListAsync(ct);
            allShareFiles.AddRange(remoteFiles);
        }
        catch
        {
            // Negeren als er geen host clients verbonden zijn
        }

        var localShareFiles = localShare.Get(path, null, ct);
        await foreach (var localFile in localShareFiles)
            allShareFiles.Add(localFile);

        return allShareFiles;
    }

    private async Task<bool> LocalEntryExists(string path, CancellationToken ct)
    {
        await foreach (var _ in localShare.Get(path, null, ct))
            return true;

        return false;
    }

    private async Task<List<ShareEntryDto>> ListShareEntries(string path, CancellationToken ct)
    {
        var allShareFiles = new List<ShareEntryDto>();
        try
        {
            var remoteFiles = await clientContext.HostHub.ToAll
                .ListDirectory(path, ct)
                .ToListAsync(ct);
            allShareFiles.AddRange(remoteFiles);
        }
        catch
        {
            // Negeren als er geen host clients verbonden zijn
        }

        var localShareFiles = localShare
            .ListDirectory(path, null, ct);
        await foreach (var localFile in localShareFiles)
            allShareFiles.Add(localFile);

        return allShareFiles;
    }

    private async Task<FileSystemEntry> CreateFileSystemEntry(
        string visiblePath,
        string readPath,
        ShareEntryDto shareFile,
        CancellationToken ct)
    {
        var fsFile = new FileSystemEntry(
            GetName(visiblePath, shareFile.Name),
            visiblePath,
            shareFile.IsDirectory,
            shareFile.Size,
            shareFile.Created,
            shareFile.LastModified);
        var entry = new Entry(fsFile, shareFile, readPath);
        await entryCollection.Responded(visiblePath, entry, ct);
        return fsFile;
    }

    private static string GetName(string path, string fallback)
    {
        path = EntryCollection.Normalize(path);
        if (string.IsNullOrEmpty(path))
            return fallback;

        var slash = path.LastIndexOf('/');
        return slash < 0 ? path : path[(slash + 1)..];
    }

    private static string GetParentPath(string path)
    {
        path = EntryCollection.Normalize(path);
        var slash = path.LastIndexOf('/');

        return slash < 0 ? string.Empty : path[..slash];
    }

}
