using gAPI.Generated;
using LanCloud.Api.Interfaces;
using LanCloud.Api.Models;
using LanCloud.Shared.Dtos;

namespace LanCloud.Api.Services;

public class FileSystem(
    IClientContext clientContext,
    ApiConfig apiConfig)
    : IFileSystem
{
    private readonly Dictionary<FileSystemEntry, ShareEntryDto> RespondedEntries = [];

    public async Task CreateDirectory(string path, CancellationToken ct = default)
    {
        if (apiConfig.StorageShare == null) return;
        apiConfig.StorageShare.CreateDirectory(path, ct);
        // Todo uit lijst verwijderde items halen.
    }

    public Task Delete(string path, CancellationToken ct = default)
    {
        // Todo lijst opslaan en daarmee filteren
        throw new NotImplementedException();
    }

    public async Task<FileSystemEntry?> Get(string path, CancellationToken ct = default)
    {
        var allShareFiles = await clientContext.HostHub.ToAll.Get(path, ct).ToListAsync();
        var localShareFiles = apiConfig.StorageShare?.Get(path, ct);
        if (localShareFiles != null)
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

        RespondedEntries[fsFile] = shareFile;
        return fsFile;
    }
    public IAsyncEnumerable<FileSystemEntry> ListDirectory(string path, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Stream?> OpenRead(string path, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task Write(string path, Stream content, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}