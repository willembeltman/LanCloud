using gAPI.Core.Ids;
using LanCloud.Shared.Dtos;
using System.Runtime.CompilerServices;

namespace LanCloud.Shared.Models;

public class LocalShare
{
    public LocalShare(string localFullName)
    {
        LocalFullName = Path.GetFullPath(localFullName);
    }

    public string LocalFullName { get; set; } = string.Empty;

    public async IAsyncEnumerable<ShareEntryDto> ListDirectory(
        string relativePath,
        SessionId? sessionId,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var path = CreateLocalFullName(relativePath);

        if (!Directory.Exists(path))
            yield break;

        foreach (var fullName in Directory.EnumerateFileSystemEntries(path))
        {
            ct.ThrowIfCancellationRequested();

            var entry = CreateEntry(fullName, relativePath, sessionId);

            yield return entry;

            // Directory.EnumerateFileSystemEntries is synchronous,
            // dus dit geeft de enumerator af en voorkomt dat een
            // enorme directory volledig synchroon doorloopt.
            await Task.Yield();
        }
    }

    public async IAsyncEnumerable<ShareEntryDto> Get(
        string relativeFullName,
        SessionId? sessionId,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var path = CreateLocalFullName(relativeFullName);

        if (File.Exists(path) || Directory.Exists(path))
        {
            ct.ThrowIfCancellationRequested();

            yield return CreateEntry(
                path,
                Path.GetDirectoryName(relativeFullName) ?? string.Empty,
                sessionId);
        }

        await Task.CompletedTask;
    }

    public async IAsyncEnumerable<FileChunkDto> ReadFile(
        string relativeFullName,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var path = CreateLocalFullName(relativeFullName);

        if (!File.Exists(path))
            yield break;

        const int chunkSize = 1 * 1024 * 1024;

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: chunkSize,
            options: FileOptions.Asynchronous |
                     FileOptions.SequentialScan);

        var buffer = new byte[chunkSize];

        long offset = 0;

        while (true)
        {
            var read = await stream.ReadAsync(
                buffer.AsMemory(),
                ct);

            if (read == 0)
                yield break;

            yield return new FileChunkDto
            {
                Offset = offset,
                Data = buffer[..read]
            };

            offset += read;
        }
    }

    public Task CreateDirectory(
        string path,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        Directory.CreateDirectory(
            CreateLocalFullName(path));

        return Task.CompletedTask;
    }

    public Task Delete(
        string path,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var fullName = CreateLocalFullName(path);

        if (File.Exists(fullName))
        {
            File.Delete(fullName);
        }
        else if (Directory.Exists(fullName))
        {
            Directory.Delete(
                fullName,
                recursive: true);
        }
        else
        {
            throw new FileNotFoundException(
                "File or directory not found.",
                fullName);
        }

        return Task.CompletedTask;
    }

    public Task Move(
        string sourcePath,
        string destinationPath,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var sourceFull = CreateLocalFullName(sourcePath);
        var destFull = CreateLocalFullName(destinationPath);

        var directory = Path.GetDirectoryName(destFull);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        if (File.Exists(sourceFull))
        {
            if (File.Exists(destFull)) File.Delete(destFull);
            File.Move(sourceFull, destFull);
        }
        else if (Directory.Exists(sourceFull))
        {
            if (Directory.Exists(destFull)) Directory.Delete(destFull, recursive: true);
            Directory.Move(sourceFull, destFull);
        }

        return Task.CompletedTask;
    }

    public async Task Write(
        string path,
        Stream incomingStream,
        CancellationToken ct)
    {
        var fullName = CreateLocalFullName(path);

        var directory = Path.GetDirectoryName(fullName);

        if (directory is not null)
            Directory.CreateDirectory(directory);

        await using var diskStream = new FileStream(
            fullName,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 1 * 1024 * 1024,
            options: FileOptions.Asynchronous);

        await incomingStream.CopyToAsync(diskStream, ct);
    }

    private string CreateLocalFullName(
        string relativeName)
    {
        relativeName = relativeName
            .Replace('\\', '/')
            .Trim('/');

        var root = Path.GetFullPath(LocalFullName);

        var fullPath = Path.GetFullPath(
            Path.Combine(
                root,
                relativeName));

        // Belangrijk: voorkom ../../etc/passwd.
        var rootWithSeparator =
            root.EndsWith(Path.DirectorySeparatorChar)
                ? root
                : root + Path.DirectorySeparatorChar;

        if (!fullPath.Equals(
                root,
                StringComparison.OrdinalIgnoreCase) &&
            !fullPath.StartsWith(
                rootWithSeparator,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                "Path escapes the configured share.");
        }

        return fullPath;
    }

    private static ShareEntryDto CreateEntry(
        string fullName,
        string relativeParent,
        SessionId? sessionId)
    {
        var attributes =
            File.GetAttributes(fullName);

        var isDirectory =
            attributes.HasFlag(FileAttributes.Directory);

        var size = 0L;
        var name = string.Empty;
        DateTime created = DateTime.MinValue;
        DateTime modified = DateTime.MinValue;

        if (isDirectory)
        {
            var dirinfo = new DirectoryInfo(fullName);
            name = dirinfo.Name;
            created = dirinfo.CreationTimeUtc;
            modified = dirinfo.LastWriteTimeUtc;
        }
        else
        {
            var info = new FileInfo(fullName);
            name = info.Name;
            size = info.Length;
            created = info.CreationTimeUtc;
            modified = info.LastWriteTimeUtc;
        }

        var relativePath = string.IsNullOrEmpty(relativeParent)
            ? name
            : $"{relativeParent.Trim('/')}/{name}";

        return new ShareEntryDto
        {
            Name = name,
            Path = relativePath,
            IsDirectory = isDirectory,
            Size = size,
            Created = created,
            LastModified = modified,
            SessionId = sessionId
        };
    }
}

//public class LocalShare
//{
//    public LocalShare() { }
//    public LocalShare(string localFullName)
//    {
//        LocalFullName = localFullName;
//    }

//    public string LocalFullName { get; set; } = string.Empty;

//    public async IAsyncEnumerable<ShareEntryDto> ListDirectory(
//        string relativePath, 
//        [EnumeratorCancellation] CancellationToken ct)
//    {
//        var path = CreateLocalFullName(relativePath);
//        var list = Directory.EnumerateFileSystemEntries(path);
//        foreach (var fullName in list)
//        {
//            yield break;
//        }
//    }
//    public async IAsyncEnumerable<ShareEntryDto> Get(
//        string relativeFullName,
//        [EnumeratorCancellation] CancellationToken ct)
//    {
//        var path = CreateLocalFullName(relativeFullName);
//        var list = Directory.EnumerateFileSystemEntries(path);
//        foreach (var fullName in list)
//        {
//            yield break;
//        }
//    }
//    public async IAsyncEnumerable<FileChunkDto> ReadFile(
//        string relativeFullName, 
//        [EnumeratorCancellation] CancellationToken ct)
//    {
//        var path = CreateLocalFullName(relativeFullName);
//        var list = Directory.EnumerateFileSystemEntries(path);
//        foreach (var fullName in list)
//        {
//            yield break;
//        }
//    }

//    public async Task CreateDirectory(string path, CancellationToken ct)
//    {
//        throw new NotImplementedException();
//    }

//    public async Task Delete(string path, CancellationToken ct)
//    {
//        throw new NotImplementedException();
//    }

//    public async Task Write(string path, Stream content, CancellationToken ct)
//    {
//        throw new NotImplementedException();
//    }

//    private string CreateLocalFullName(string relativeName)
//    {
//        throw new NotImplementedException();
//    }
//}
