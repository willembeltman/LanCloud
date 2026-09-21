using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Server.Entities;
using gAPI.Generated;
using LanCloud.Api.Collections;
using LanCloud.Api.Interfaces;
using LanCloud.Api.Models;
using LanCloud.Shared.Dtos;
using LanCloud.Shared.Interfaces;
using LanCloud.Shared.Models;
using System.Runtime.CompilerServices;

namespace LanCloud.Api.Services;

internal class FileSystem(
    IClientContext clientContext,
    RespondedEntryCollection entryCollection,
    LanCloudApiConfig apiConfig) 
    : IFileSystemApi, IFileSystemDirect

{
    LocalShare LocalShare => apiConfig.LocalShare;

    public async Task<AuthenticationInfo> GetAuthenticationInfo(CancellationToken ct)
    {
        return new AuthenticationInfo(
            Required: false,
            Realm: "LanCloud");
    }

    public async Task<AuthStateUserDto?> AuthenticateUser(string? userName, string? password, CancellationToken ct)
    {
        return new AuthStateUserDto()
        {
            Email = userName ?? "",
            UserName = userName ?? ""
        };
    }

    public Task CreateDirectory(string path, CancellationToken ct)
    {
        return LocalShare.CreateDirectory(path, ct);
    }

    public async Task Delete(string path, CancellationToken ct)
    {
        if (await LocalShare.Exist(path, ct))
        {
            await LocalShare.Delete(path, ct);
            return;
        }

        throw new InvalidOperationException(
            "Remote files are read-only.");
    }

    public async Task Move(string sourcePath, string destinationPath, CancellationToken ct)
    {
        if (!await LocalShare.Exist(sourcePath, ct))
        {
            throw new InvalidOperationException(
                "Remote files are read-only.");
        }

        await LocalShare.Move(
            sourcePath,
            destinationPath,
            ct);
    }

    public async Task<FileSystemEntry?> Get(string path, CancellationToken ct)
    {
        path = RespondedEntryCollection.Normalize(path);

        if (string.IsNullOrEmpty(path))
        {
            var rootFsEntry = new FileSystemEntry(
                "",
                "",
                true,
                0,
                DateTime.UtcNow,
                DateTime.UtcNow);

            var rootShareEntry = new HubEntryDto("", "", true, 0, DateTime.Now, DateTime.Now, null);

            var rootEntry = new RespondedEntry(
                rootFsEntry,
                rootShareEntry,
                "");

            entryCollection.Responded("", rootEntry);

            return rootFsEntry;
        }

        var remoteTask = clientContext.HostHub
            .ToAll
            .Get(path, ct)
            .ToArrayAsync(ct)
            .AsTask();

        var localTask = LocalShare
            .Get(path, null, ct)
            .ToArrayAsync(ct)
            .AsTask();

        await Task.WhenAll(remoteTask, localTask);

        var allShareFiles = remoteTask.Result.Concat(localTask.Result);

        var shareFile = allShareFiles
            .OrderByDescending(x => x.GetLastDate())
            .FirstOrDefault();

        if (shareFile == null)
            return null;

        return await CreateFileSystemEntry(
            path,
            path,
            shareFile,
            ct);
    }

    public async IAsyncEnumerable<FileSystemEntry> ListDirectory(string path, [EnumeratorCancellation] CancellationToken ct)
    {
        path = RespondedEntryCollection.Normalize(path);

        var remoteTask = clientContext.HostHub
            .ToAll
            .ListDirectory(path, ct)
            .ToArrayAsync(ct)
            .AsTask();

        var localTask = LocalShare
            .ListDirectory(path, null, ct)
            .ToArrayAsync(ct)
            .AsTask();

        await Task.WhenAll(remoteTask, localTask);

        var files = remoteTask.Result.Concat(localTask.Result);

        foreach (var shareFile in files
            .GroupBy(x => x.Path)
            .Select(x => x.OrderByDescending(y => y.GetLastDate()).First()))
        {
            yield return await CreateFileSystemEntry(
                shareFile.Path,
                shareFile.Path,
                shareFile,
                ct);
        }
    }

    public async Task<Stream?> OpenRead(string path, CancellationToken ct)
    {
        if (!entryCollection.TryGet(path, out var entry) ||
            entry == null)
        {
            var fetched = await Get(path, ct);

            if (fetched == null ||
                !entryCollection.TryGet(path, out entry) ||
                entry == null)
            {
                return null;
            }
        }

        IAsyncEnumerable<DataChunkDto> OpenChunks(
            long startOffset,
            CancellationToken streamCt)
        {
            if (entry.ShareEntryDto.SessionId == null)
            {
                return LocalShare.ReadFile(
                    entry.Path,
                    startOffset,
                    streamCt);
            }

            return clientContext.HostHub
                .ToSession(entry.ShareEntryDto.SessionId)
                .ReadFile(
                    entry.Path,
                    startOffset,
                    streamCt);
        }

        return new DataChunkStreamSeekableReader(
            OpenChunks,
            entry.FileSystemEntry.Size,
            ct);
    }
    public Task Write(string path, Stream stream, CancellationToken ct)
    {
        return LocalShare.Write(path, stream, ct);
    }
    public Task Append(string path, Stream stream, CancellationToken ct)
    {
        return LocalShare.Append(path, stream, ct);
    }

    async IAsyncEnumerable<byte[]> IFileSystemApi.OpenRead(string path, long startOffset, [EnumeratorCancellation] CancellationToken ct)
    {
        var buffer = new byte[8 * 1024 * 1024];
        using var stream = await OpenRead(path, ct);
        if (stream == null)
            yield break;
        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(), ct);
            if (read <= 0)
                yield break;

            yield return buffer[..read];
        }
    }
    Task IFileSystemApi.Write(string path, long startOffset, IAsyncEnumerable<byte[]> stream, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
    Task IFileSystemApi.Append(string path, IAsyncEnumerable<byte[]> stream, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    private async Task<FileSystemEntry> CreateFileSystemEntry(
        string visiblePath,
        string readPath,
        HubEntryDto shareFile,
        CancellationToken ct)
    {
        var fsFile = new FileSystemEntry(
            GetName(visiblePath, shareFile.Name),
            visiblePath,
            shareFile.IsDirectory,
            shareFile.Size,
            shareFile.Created,
            shareFile.LastModified);
        var entry = new RespondedEntry(fsFile, shareFile, readPath);
        entryCollection.Responded(visiblePath, entry);
        return fsFile;
    }

    private static string GetName(string path, string fallback)
    {
        path = RespondedEntryCollection.Normalize(path);
        if (string.IsNullOrEmpty(path))
            return fallback;

        var slash = path.LastIndexOf('/');
        return slash < 0 ? path : path[(slash + 1)..];
    }

}
