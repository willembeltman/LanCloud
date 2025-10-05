using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using DokanNet;
using LanCloud.Shared.Interfaces;
using FileAccess = DokanNet.FileAccess;

namespace LanCloud.VirtualDrive
{
    internal sealed class LanCloudDriveOperations : IDokanOperations
    {
        private readonly ILanCloudFileSystem _fileSystem;
        private readonly LanCloudDriveMountOptions _options;
        private readonly ConcurrentDictionary<long, OpenFileHandle> _handles = new ConcurrentDictionary<long, OpenFileHandle>();
        private long _handleId;

        public LanCloudDriveOperations(ILanCloudFileSystem fileSystem, LanCloudDriveMountOptions options)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        private bool IsReadOnly => _options.ReadOnly;

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
            var id = System.Threading.Interlocked.Increment(ref _handleId);
            handle.Id = id;
            info.Context = handle;
            _handles[id] = handle;
            return handle;
        }

        private static OpenFileHandle GetHandle(IDokanFileInfo info)
            => info.Context as OpenFileHandle;

        private void CloseHandle(IDokanFileInfo info)
        {
            if (info.Context is OpenFileHandle handle)
            {
                if (_handles.TryRemove(handle.Id, out var stored))
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
                    if (_fileSystem.DirectoryExists(path))
                    {
                        return DokanResult.AlreadyExists;
                    }

                    if (IsReadOnly)
                    {
                        return DokanResult.AccessDenied;
                    }

                    _fileSystem.CreateDirectory(path);
                    return DokanResult.Success;
                }

                if (!_fileSystem.DirectoryExists(path))
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

            var fileExists = _fileSystem.FileExists(path);

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

                    var createHandle = new OpenFileHandle(path, _fileSystem.OpenWrite(path, FileMode.CreateNew), writable: true);
                    RegisterHandle(info, createHandle);
                    return DokanResult.Success;

                case FileMode.Create:
                case FileMode.Truncate:
                    if (IsReadOnly)
                    {
                        return DokanResult.AccessDenied;
                    }

                    var overwriteHandle = new OpenFileHandle(path, _fileSystem.OpenWrite(path, FileMode.Create), writable: true);
                    RegisterHandle(info, overwriteHandle);
                    return DokanResult.Success;

                case FileMode.OpenOrCreate:
                    if (!fileExists)
                    {
                        if (IsReadOnly)
                        {
                            return DokanResult.AccessDenied;
                        }

                        var openOrCreateHandle = new OpenFileHandle(path, _fileSystem.OpenWrite(path, FileMode.CreateNew), writable: true);
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

                        var appendHandle = new OpenFileHandle(path, _fileSystem.OpenWrite(path, FileMode.CreateNew), writable: true);
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

            var stream = _fileSystem.OpenRead(path);
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
                            _fileSystem.DeleteDirectory(handle.Path);
                        }
                    }
                    else if (!IsReadOnly)
                    {
                        _fileSystem.DeleteFile(handle.Path);
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

            if (!handle.EnsureOffset(offset, _fileSystem))
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

            ILanCloudFileSystemEntry entry = null;
            if (_fileSystem.FileExists(path))
            {
                entry = _fileSystem.GetFile(path);
            }
            else if (_fileSystem.DirectoryExists(path))
            {
                entry = _fileSystem.GetDirectory(path);
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
            if (!_fileSystem.DirectoryExists(path))
            {
                files = Array.Empty<FileInformation>();
                return DokanResult.FileNotFound;
            }

            files = _fileSystem.EnumerateDirectory(path)
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
            if (!_fileSystem.DirectoryExists(path))
            {
                files = Array.Empty<FileInformation>();
                return DokanResult.FileNotFound;
            }

            files = _fileSystem.EnumerateDirectory(path)
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


        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
        {
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
            if (_fileSystem.FileExists(path))
            {
                _fileSystem.SetFileTimestamps(path, creationTime, lastAccessTime, lastWriteTime);
                return DokanResult.Success;
            }

            if (_fileSystem.DirectoryExists(path))
            {
                _fileSystem.SetDirectoryTimestamps(path, creationTime, lastAccessTime, lastWriteTime);
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
            if (!_fileSystem.FileExists(path))
            {
                return DokanResult.FileNotFound;
            }

            _fileSystem.DeleteFile(path);
            return DokanResult.Success;
        }

        public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
        {
            if (IsReadOnly)
            {
                return DokanResult.AccessDenied;
            }

            var path = NormalizePath(fileName);
            if (!_fileSystem.DirectoryExists(path))
            {
                return DokanResult.PathNotFound;
            }

            _fileSystem.DeleteDirectory(path);
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

            if (_fileSystem.DirectoryExists(source))
            {
                if (!replace && (_fileSystem.DirectoryExists(destination) || _fileSystem.FileExists(destination)))
                {
                    return DokanResult.AlreadyExists;
                }

                _fileSystem.MoveDirectory(source, destination);
                return DokanResult.Success;
            }

            if (!_fileSystem.FileExists(source))
            {
                return DokanResult.FileNotFound;
            }

            if (!replace && _fileSystem.FileExists(destination))
            {
                return DokanResult.AlreadyExists;
            }

            _fileSystem.MoveFile(source, destination);
            return DokanResult.Success;
        }

        public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
        {
            return DokanResult.NotImplemented;
        }

        public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
        {
            return DokanResult.NotImplemented;
        }

        public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            return DokanResult.Success;
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            return DokanResult.Success;
        }

        public NtStatus GetDiskFreeSpace(
            out long freeBytesAvailable,
            out long totalNumberOfBytes,
            out long totalNumberOfFreeBytes,
            IDokanFileInfo info)
        {
            _fileSystem.GetDiskFreeSpace(out freeBytesAvailable, out totalNumberOfBytes, out totalNumberOfFreeBytes);
            return DokanResult.Success;
        }

        public NtStatus GetVolumeInformation(
            out string volumeLabel,
            out FileSystemFeatures features,
            out string fileSystemName,
            out uint maximumComponentLength,
            IDokanFileInfo info)
        {
            volumeLabel = _options.VolumeLabel;
            fileSystemName = "LANCloud";
            maximumComponentLength = 255;
            features = FileSystemFeatures.CasePreservedNames |
                       FileSystemFeatures.CaseSensitiveSearch;// |
                       //FileSystemFeatures.Namespaces;
            return DokanResult.Success;
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
        {
            security = null;
            return DokanResult.NotImplemented;
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
        {
            return DokanResult.NotImplemented;
        }

        public NtStatus Mounted(string mountPoint, IDokanFileInfo info)
        {
            try
            {
                // Eventueel loggen of callback naar je LanCloud systeem
                Console.WriteLine($"[LanCloud] Drive mounted at {mountPoint}");

                // Als je ILanCloudFileSystem een hook heeft (bv. OnMounted), kun je die hier aanroepen.
                // _fileSystem.OnMounted?.Invoke(mountPoint);

                return DokanResult.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LanCloud] Mounted callback failed: {ex}");
                return DokanResult.Error;
            }
        }



        public NtStatus Unmounted(IDokanFileInfo info)
        {
            foreach (var handle in _handles.Values.ToArray())
            {
                handle.Dispose();
            }

            _handles.Clear();
            return DokanResult.Success;
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
        {
            streams = Array.Empty<FileInformation>();
            return DokanResult.Success;
        }



        private sealed class OpenFileHandle : IDisposable
        {
            private Stream _stream;

            private OpenFileHandle(string path, Stream stream, bool writable, bool isDirectory)
            {
                Path = path;
                _stream = stream;
                CanWrite = writable;
                IsDirectoryHandle = isDirectory;
            }

            public OpenFileHandle(string path, Stream stream, bool writable)
                : this(path, stream, writable, false)
            {
            }

            private OpenFileHandle(string path)
                : this(path, stream: null, writable: false, isDirectory: true)
            {
            }

            public static OpenFileHandle Directory(string path) => new OpenFileHandle(path);

            public long Id { get; set; }
            public string Path { get; }
            public bool CanWrite { get; }
            public bool CanRead => !CanWrite && !IsDirectoryHandle;
            public bool IsDirectoryHandle { get; }
            public Stream Stream => _stream ?? throw new InvalidOperationException("Directory handles do not expose streams.");

            public bool EnsureOffset(long offset, ILanCloudFileSystem fileSystem)
            {
                if (_stream == null)
                {
                    return offset == 0;
                }

                if (offset == _stream.Position)
                {
                    return true;
                }

                if (offset < _stream.Position)
                {
                    _stream.Dispose();
                    _stream = fileSystem.OpenRead(Path);
                }

                var buffer = new byte[8192];
                while (_stream.Position < offset)
                {
                    var remaining = offset - _stream.Position;
                    var read = _stream.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                    if (read == 0)
                    {
                        break;
                    }
                }

                return _stream.Position == offset;
            }

            public void Dispose()
            {
                _stream?.Dispose();
                _stream = null;
            }
        }
    }
}
