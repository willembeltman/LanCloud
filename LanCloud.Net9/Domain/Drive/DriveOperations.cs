using DokanNet;
using LanCloud.Domain.Application;
using System.Collections.Concurrent;
using System.Security.AccessControl;
using FileAccess = DokanNet.FileAccess;

namespace LanCloud.Domain.Drive;

internal sealed class DriveOperations : IDokanOperations
{
    private readonly DriveServer DriveServer;

    private readonly ConcurrentDictionary<long, OpenFileHandle> Handles = new ConcurrentDictionary<long, OpenFileHandle>();
    private long HandleId;

    public DriveOperations(DriveServer driveServer)
    {
        DriveServer = driveServer ?? throw new ArgumentNullException(nameof(driveServer));

        //driveServer.Application.FileServer
    }

    private FileSystem FileSystem => DriveServer.FileSystem;
    private DriveMountOptions Options => DriveServer.Options;

    private bool IsReadOnly => Options.ReadOnly;

    private static string NormalizePath(string fileName)
    {
        if (string.IsNullOrEmpty(fileName) || fileName == "\\")
        {
            return "/";
        }

        var normalized = fileName.Replace('\\', '/');
        if (!normalized.StartsWith("/", StringComparison.Ordinal))
        {
            normalized = "/" + normalized.TrimStart('/');
        }

        return normalized;
    }

    private OpenFileHandle RegisterHandle(IDokanFileInfo info, OpenFileHandle handle)
    {
        var id = Interlocked.Increment(ref HandleId);
        handle.Id = id;
        info.Context = handle;
        Handles[id] = handle;
        return handle;
    }

    private static OpenFileHandle? GetHandle(IDokanFileInfo info)
        => info.Context as OpenFileHandle;

    private void CloseHandle(IDokanFileInfo info)
    {
        if (info.Context is OpenFileHandle handle)
        {
            if (Handles.TryRemove(handle.Id, out var stored))
            {
                stored.Dispose();
            }

            info.Context = null;
        }
    }

    public NtStatus CreateFile(
        string fileName,
        FileAccess access,
        FileShare share,
        FileMode mode,
        FileOptions options,
        FileAttributes attributes,
        IDokanFileInfo info)

    {
        var path = NormalizePath(fileName);
        var isDirectoryRequest = info.IsDirectory;

        if (isDirectoryRequest)
        {
            info.IsDirectory = true;

            if (mode == FileMode.CreateNew)
            {
                if (FileSystem.DirectoryExists(path))
                {
                    return DokanResult.AlreadyExists;
                }

                if (IsReadOnly)
                {
                    return DokanResult.AccessDenied;
                }

                FileSystem.CreateDirectory(path);
                return DokanResult.Success;
            }

            if (!FileSystem.DirectoryExists(path))
            {
                return DokanResult.PathNotFound;
            }

            RegisterHandle(info, OpenFileHandle.Directory(path));
            return DokanResult.Success;
        }

        var requestingWrite = (access & (FileAccess.GenericWrite | FileAccess.WriteData | FileAccess.AppendData)) != 0;
        if (requestingWrite && IsReadOnly)
        {
            return DokanResult.AccessDenied;
        }

        var fileExists = FileSystem.FileExists(path);

        switch (mode)
        {
            case FileMode.CreateNew:
                if (fileExists)
                {
                    return DokanResult.AlreadyExists;
                }

                if (IsReadOnly)
                {
                    return DokanResult.AccessDenied;
                }

                var createHandle = new OpenFileHandle(path, FileSystem.OpenWrite(path, FileMode.CreateNew), writable: true);
                RegisterHandle(info, createHandle);
                return DokanResult.Success;

            case FileMode.Create:
            case FileMode.Truncate:
                if (IsReadOnly)
                {
                    return DokanResult.AccessDenied;
                }

                var overwriteHandle = new OpenFileHandle(path, FileSystem.OpenWrite(path, FileMode.Create), writable: true);
                RegisterHandle(info, overwriteHandle);
                return DokanResult.Success;

            case FileMode.OpenOrCreate:
                if (!fileExists)
                {
                    if (IsReadOnly)
                    {
                        return DokanResult.AccessDenied;
                    }

                    var openOrCreateHandle = new OpenFileHandle(path, FileSystem.OpenWrite(path, FileMode.CreateNew), writable: true);
                    RegisterHandle(info, openOrCreateHandle);
                    return DokanResult.Success;
                }
                break;

            case FileMode.Append:
                if (!fileExists)
                {
                    if (IsReadOnly)
                    {
                        return DokanResult.AccessDenied;
                    }

                    var appendHandle = new OpenFileHandle(path, FileSystem.OpenWrite(path, FileMode.CreateNew), writable: true);
                    RegisterHandle(info, appendHandle);
                    return DokanResult.Success;
                }
                break;
        }

        if (!fileExists)
        {
            return DokanResult.FileNotFound;
        }

        if (requestingWrite)
        {
            if (IsReadOnly)
            {
                return DokanResult.AccessDenied;
            }

            // Existing file random updates are not supported.
            return DokanResult.NotImplemented;
        }

        var stream = FileSystem.OpenRead(path);
        RegisterHandle(info, new OpenFileHandle(path, stream, writable: false));
        return DokanResult.Success;
    }

    public void Cleanup(string fileName, IDokanFileInfo info)
    {
        var handle = GetHandle(info);
        if (handle != null)
        {
            if (info.DeletePending)
            {
                if (handle.IsDirectoryHandle)
                {
                    if (!IsReadOnly)
                    {
                        FileSystem.DeleteDirectory(handle.Path);
                    }
                }
                else if (!IsReadOnly)
                {
                    FileSystem.DeleteFile(handle.Path);
                }
            }

            CloseHandle(info);
        }
    }

    public void CloseFile(string fileName, IDokanFileInfo info)
    {
        CloseHandle(info);
    }

    public NtStatus ReadFile(
        string fileName,
        byte[] buffer,
        out int bytesRead,
        long offset,
        IDokanFileInfo info)
    {
        bytesRead = 0;
        var handle = GetHandle(info);
        if (handle == null)
        {
            return DokanResult.InvalidHandle;
        }

        if (!handle.CanRead)
        {
            return DokanResult.AccessDenied;
        }

        if (!handle.EnsureOffset(offset, FileSystem))
        {
            return DokanResult.Error;
        }

        bytesRead = handle.Stream.Read(buffer, 0, buffer.Length);
        return DokanResult.Success;
    }

    public NtStatus WriteFile(
        string fileName,
        byte[] buffer,
        out int bytesWritten,
        long offset,
        IDokanFileInfo info)
    {
        bytesWritten = 0;
        var handle = GetHandle(info);
        if (handle == null)
        {
            return DokanResult.InvalidHandle;
        }

        if (!handle.CanWrite)
        {
            return DokanResult.AccessDenied;
        }

        if (offset != handle.Stream.Position)
        {
            return DokanResult.NotImplemented;
        }

        handle.Stream.Write(buffer, 0, buffer.Length);
        bytesWritten = buffer.Length;
        return DokanResult.Success;
    }

    public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info)
    {
        var handle = GetHandle(info);
        if (handle?.Stream != null && handle.CanWrite)
        {
            handle.Stream.Flush();
        }

        return DokanResult.Success;
    }

    public NtStatus GetFileInformation(
        string fileName,
        out FileInformation fileInfo,
        IDokanFileInfo info)
    {
        var path = NormalizePath(fileName);
        fileInfo = new FileInformation
        {
            FileName = Path.GetFileName(fileName)
        };

        DriveFileSystemEntry? entry = null;
        if (FileSystem.FileExists(path))
        {
            entry = FileSystem.GetFile(path);
        }
        else if (FileSystem.DirectoryExists(path))
        {
            entry = FileSystem.GetDirectory(path);
        }

        if (entry == null)
        {
            return DokanResult.FileNotFound;
        }

        fileInfo.Attributes = entry.Attributes;
        fileInfo.CreationTime = entry.CreationTime;
        fileInfo.LastAccessTime = entry.LastAccessTime;
        fileInfo.LastWriteTime = entry.LastWriteTime;
        fileInfo.Length = entry.Length ?? 0;
        return DokanResult.Success;
    }

    public NtStatus FindFiles(
        string fileName,
        out IList<FileInformation> files,
        IDokanFileInfo info)
    {
        var path = NormalizePath(fileName);
        if (!FileSystem.DirectoryExists(path))
        {
            files = Array.Empty<FileInformation>();
            return DokanResult.FileNotFound;
        }

        files = FileSystem.EnumerateDirectory(path)
            .Select(entry => new FileInformation
            {
                FileName = entry.Name,
                Attributes = entry.Attributes,
                CreationTime = entry.CreationTime,
                LastAccessTime = entry.LastAccessTime,
                LastWriteTime = entry.LastWriteTime,
                Length = entry.Length ?? 0
            })
            .ToList();

        return DokanResult.Success;
    }
    public NtStatus FindFilesWithPattern(
        string fileName,
        string searchPattern,
        out IList<FileInformation> files,
        IDokanFileInfo info)
    {
        var path = NormalizePath(fileName);
        if (!FileSystem.DirectoryExists(path))
        {
            files = Array.Empty<FileInformation>();
            return DokanResult.FileNotFound;
        }

        files = FileSystem.EnumerateDirectory(path)
            .Select(entry => new FileInformation
            {
                FileName = entry.Name,
                Attributes = entry.Attributes,
                CreationTime = entry.CreationTime,
                LastAccessTime = entry.LastAccessTime,
                LastWriteTime = entry.LastWriteTime,
                Length = entry.Length ?? 0
            })
            .ToList();

        return DokanResult.Success;
    }

    public NtStatus SetFileTime(
        string fileName,
        DateTime? creationTime,
        DateTime? lastAccessTime,
        DateTime? lastWriteTime,
        IDokanFileInfo info)
    {
        var path = NormalizePath(fileName);
        if (FileSystem.FileExists(path))
        {
            FileSystem.SetFileTimestamps(path, creationTime, lastAccessTime, lastWriteTime);
            return DokanResult.Success;
        }

        if (FileSystem.DirectoryExists(path))
        {
            FileSystem.SetDirectoryTimestamps(path, creationTime, lastAccessTime, lastWriteTime);
            return DokanResult.Success;
        }

        return DokanResult.FileNotFound;
    }

    public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
    {
        if (IsReadOnly)
        {
            return DokanResult.AccessDenied;
        }

        var path = NormalizePath(fileName);
        if (!FileSystem.FileExists(path))
        {
            return DokanResult.FileNotFound;
        }

        FileSystem.DeleteFile(path);
        return DokanResult.Success;
    }

    public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
    {
        if (IsReadOnly)
        {
            return DokanResult.AccessDenied;
        }

        var path = NormalizePath(fileName);
        if (!FileSystem.DirectoryExists(path))
        {
            return DokanResult.PathNotFound;
        }

        FileSystem.DeleteDirectory(path);
        return DokanResult.Success;
    }

    public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info)
    {
        if (IsReadOnly)
        {
            return DokanResult.AccessDenied;
        }

        var source = NormalizePath(oldName);
        var destination = NormalizePath(newName);

        if (FileSystem.DirectoryExists(source))
        {
            if (!replace && (FileSystem.DirectoryExists(destination) || FileSystem.FileExists(destination)))
            {
                return DokanResult.AlreadyExists;
            }

            FileSystem.MoveDirectory(source, destination);
            return DokanResult.Success;
        }

        if (!FileSystem.FileExists(source))
        {
            return DokanResult.FileNotFound;
        }

        if (!replace && FileSystem.FileExists(destination))
        {
            return DokanResult.AlreadyExists;
        }

        FileSystem.MoveFile(source, destination);
        return DokanResult.Success;
    }

    public NtStatus GetDiskFreeSpace(
        out long freeBytesAvailable,
        out long totalNumberOfBytes,
        out long totalNumberOfFreeBytes,
        IDokanFileInfo info)
    {
        FileSystem.GetDiskFreeSpace(out freeBytesAvailable, out totalNumberOfBytes, out totalNumberOfFreeBytes);
        return DokanResult.Success;
    }

    public NtStatus GetVolumeInformation(
        out string volumeLabel,
        out FileSystemFeatures features,
        out string fileSystemName,
        out uint maximumComponentLength,
        IDokanFileInfo info)
    {
        volumeLabel = Options.VolumeLabel;
        fileSystemName = "LANCloud";
        maximumComponentLength = 255;
        features = FileSystemFeatures.CasePreservedNames |
                   FileSystemFeatures.CaseSensitiveSearch;// |
                                                          //FileSystemFeatures.Namespaces;
        return DokanResult.Success;
    }


    public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
    {
        return DokanResult.Success;
    }

    public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
    {
        return DokanResult.Success;
    }

    public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
    {
        return DokanResult.Success;
    }

    public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity? security, AccessControlSections sections, IDokanFileInfo info)
    {
        security = null;
        return DokanResult.NotImplemented;
    }

    public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
    {
        return DokanResult.NotImplemented;
    }

    public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
    {
        return DokanResult.NotImplemented;
    }

    public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
    {
        return DokanResult.NotImplemented;
    }


    public NtStatus Mounted(string mountPoint, IDokanFileInfo info)
    {
        DriveServer.MountStatus = DokanStatus.Success;
        return DokanResult.Success;
    }

    public NtStatus Unmounted(IDokanFileInfo info)
    {
        foreach (var handle in Handles.Values.ToArray())
        {
            handle.Dispose();
        }

        Handles.Clear();
        return DokanResult.Success;
    }

    public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
    {
        streams = Array.Empty<FileInformation>();
        return DokanResult.Success;
    }
}