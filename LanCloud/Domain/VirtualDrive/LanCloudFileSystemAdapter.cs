using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanCloud.Domain.Application;
using LanCloud.Domain.FileRef;
using LanCloud.Shared.Interfaces;
using LanCloud.VirtualDrive;

namespace LanCloud.Domain.VirtualDrive
{
    public sealed class LanCloudFileSystemAdapter : ILanCloudFileSystem
    {
        private readonly LocalApplication _application;
        private readonly ILogger _logger;

        public LanCloudFileSystemAdapter(LocalApplication application, ILogger logger)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path == "/")
            {
                return "/";
            }

            path = path.Replace('\\', '/');
            if (!path.StartsWith("/", StringComparison.Ordinal))
            {
                path = "/" + path;
            }

            return path.TrimEnd('/');
        }

        private DirectoryInfo ResolveDirectoryInfo(string path)
        {
            path = NormalizePath(path);
            var fullName = PathTranslator.TranslateDirectoryPathToFullName(_application.RealRoot, path);
            return new DirectoryInfo(fullName);
        }

        private FileInfo ResolveFileInfo(string path)
        {
            path = NormalizePath(path);
            var fullName = PathTranslator.TranslatePathToFullName(_application.RealRoot, path);
            return new FileInfo(fullName);
        }

        public bool DirectoryExists(string path)
            => ResolveDirectoryInfo(path).Exists;

        public bool FileExists(string path)
        {
            var file = new LocalFileRef(_application, NormalizePath(path), _logger);
            return file.Exists;
        }

        public ILanCloudFileSystemEntry GetDirectory(string path)
        {
            var directoryInfo = ResolveDirectoryInfo(path);
            if (!directoryInfo.Exists)
            {
                return null;
            }

            var normalized = NormalizePath(path);
            return new LanCloudFileSystemEntry(
                normalized,
                directoryInfo.Name,
                isDirectory: true,
                length: null,
                creationTime: directoryInfo.CreationTime,
                lastAccessTime: directoryInfo.LastAccessTime,
                lastWriteTime: directoryInfo.LastWriteTime,
                attributes: FileAttributes.Directory);
        }

        public ILanCloudFileSystemEntry GetFile(string path)
        {
            var file = new LocalFileRef(_application, NormalizePath(path), _logger);
            if (!file.Exists || file.Metadata == null)
            {
                return null;
            }

            return new LanCloudFileSystemEntry(
                file.Path,
                file.Name,
                isDirectory: false,
                length: file.Length,
                creationTime: file.RealInfo.CreationTime,
                lastAccessTime: file.RealInfo.LastAccessTime,
                lastWriteTime: file.RealInfo.LastWriteTime,
                attributes: FileAttributes.Archive);
        }

        public IEnumerable<ILanCloudFileSystemEntry> EnumerateDirectory(string path)
        {
            var directory = new LocalFileRefDirectory(_application, NormalizePath(path), _logger);
            if (!directory.Exists)
            {
                return Enumerable.Empty<LanCloudFileSystemEntry>();
            }

            var directories = directory.GetDirectories()
                .Select(dir => new LanCloudFileSystemEntry(
                    dir.Path,
                    dir.Name,
                    isDirectory: true,
                    length: null,
                    creationTime: dir.LastWriteTime,
                    lastAccessTime: dir.LastWriteTime,
                    lastWriteTime: dir.LastWriteTime,
                    attributes: FileAttributes.Directory));

            var files = directory.GetFiles()
                .Where(file => file.Metadata != null)
                .Select(file => new LanCloudFileSystemEntry(
                    file.Path,
                    file.Name,
                    isDirectory: false,
                    length: file.Length,
                    creationTime: file.RealInfo.CreationTime,
                    lastAccessTime: file.RealInfo.LastAccessTime,
                    lastWriteTime: file.RealInfo.LastWriteTime,
                    attributes: FileAttributes.Archive));

            return directories.Concat(files);
        }

        public void CreateDirectory(string path)
        {
            var directory = new LocalFileRefDirectory(_application, NormalizePath(path), _logger);
            if (!directory.Exists)
            {
                directory.Create();
            }
        }

        public void DeleteDirectory(string path)
        {
            var directory = new LocalFileRefDirectory(_application, NormalizePath(path), _logger);
            if (directory.Exists)
            {
                directory.Delete();
            }
        }

        public void MoveDirectory(string sourcePath, string destinationPath)
        {
            var directory = new LocalFileRefDirectory(_application, NormalizePath(sourcePath), _logger);
            if (!directory.Exists)
            {
                return;
            }

            directory.MoveTo(NormalizePath(destinationPath));
        }

        public void MoveFile(string sourcePath, string destinationPath)
        {
            var file = new LocalFileRef(_application, NormalizePath(sourcePath), _logger);
            if (!file.Exists)
            {
                return;
            }

            file.MoveTo(NormalizePath(destinationPath));
        }

        public Stream OpenRead(string path)
        {
            var file = new LocalFileRef(_application, NormalizePath(path), _logger);
            return file.OpenRead() ?? throw new IOException($"File {path} could not be opened for reading.");
        }

        public Stream OpenWrite(string path, FileMode mode)
        {
            var normalized = NormalizePath(path);
            var file = new LocalFileRef(_application, normalized, _logger);

            switch (mode)
            {
                case FileMode.Create:
                case FileMode.CreateNew:
                case FileMode.Truncate:
                    return file.Create();
                case FileMode.Append:
                    return file.OpenAppend();
                default:
                    throw new NotSupportedException($"FileMode {mode} is not supported.");
            }
        }

        public void DeleteFile(string path)
        {
            var file = new LocalFileRef(_application, NormalizePath(path), _logger);
            if (file.Exists)
            {
                file.Delete();
            }
        }

        public void SetDirectoryTimestamps(string path, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime)
        {
            var info = ResolveDirectoryInfo(path);
            if (!info.Exists)
            {
                return;
            }

            if (creationTime.HasValue)
            {
                info.CreationTime = creationTime.Value;
            }
            if (lastAccessTime.HasValue)
            {
                info.LastAccessTime = lastAccessTime.Value;
            }
            if (lastWriteTime.HasValue)
            {
                info.LastWriteTime = lastWriteTime.Value;
            }
        }

        public void SetFileTimestamps(string path, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime)
        {
            var info = ResolveFileInfo(path);
            if (!info.Exists)
            {
                return;
            }

            if (creationTime.HasValue)
            {
                info.CreationTime = creationTime.Value;
            }
            if (lastAccessTime.HasValue)
            {
                info.LastAccessTime = lastAccessTime.Value;
            }
            if (lastWriteTime.HasValue)
            {
                info.LastWriteTime = lastWriteTime.Value;
            }
        }

        public void GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes)
        {
            var rootPath = _application.RealRoot.FullName;
            var drive = new DriveInfo(Path.GetPathRoot(rootPath));
            freeBytesAvailable = drive.AvailableFreeSpace;
            totalNumberOfBytes = drive.TotalSize;
            totalNumberOfFreeBytes = drive.TotalFreeSpace;
        }
    }
}

