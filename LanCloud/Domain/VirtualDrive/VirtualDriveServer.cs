using DokanNet;
using LanCloud.Domain.Application;
using LanCloud.Domain.FileRef;
using LanCloud.Models.Configs;
using LanCloud.Interfaces;
using LanCloud.Servers.VirtualDrive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LanCloud.Domain.VirtualDrive
{
    public class VirtualDriveServer : IDriveFileSystem, IDisposable
    {
        public VirtualDriveServer(LocalApplication application, ILogger logger)
        {
            Application = application;
            Logger = logger;

            var mountPoint = string.IsNullOrWhiteSpace(Config.VirtualDriveMountPoint)
                ? "N:\\"
                : Config.VirtualDriveMountPoint;
            var volumeLabel = string.IsNullOrWhiteSpace(Config.VirtualDriveVolumeLabel)
                ? "LANCloud"
                : Config.VirtualDriveVolumeLabel;

            try
            {
                var mountOptions = new DriveMountOptions(mountPoint, volumeLabel, Config.VirtualDriveReadOnly);
                DriveServer = new DriveServer(mountOptions, this, application, logger);
                DriveServer.Start();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                DriveServer?.Dispose();
                DriveServer = null;
            }

            Logger.Info($"Loaded");
        }

        public LocalApplication Application { get; }
        public ILogger Logger { get; }
        public DriveServer DriveServer { get; }

        public ApplicationConfig Config => Application.Config;
        public DokanStatus MountStatus => DriveServer.MountStatus;
        public bool IsRunning => DriveServer.MountStatus == DokanNet.DokanStatus.Success;

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

            var normalizedPath = path.TrimEnd('/');
            return normalizedPath;
        }
        private DirectoryInfo ResolveDirectoryInfo(string path)
        {
            path = NormalizePath(path);
            var fullName = PathTranslator.TranslateDirectoryPathToFullName(Application.RealRoot, path);
            var dirInfo = new DirectoryInfo(fullName);
            return dirInfo;
        }
        private FileInfo ResolveFileInfo(string path)
        {
            path = NormalizePath(path);
            var fullName = PathTranslator.TranslatePathToFullName(Application.RealRoot, path);
            var fileInfo = new FileInfo(fullName);
            return fileInfo;
        }
        public bool DirectoryExists(string path)
        {
            var dir = ResolveDirectoryInfo(path);
            var exist = dir.Exists;
            return exist;
        }

        public bool FileExists(string path)
        {
            var file = new LocalFileRef(Application, NormalizePath(path), Logger);
            var exist = file.Exists;
            return exist;
        }
        public IDriveFileSystemEntry GetDirectory(string path)
        {
            var directoryInfo = ResolveDirectoryInfo(path);
            if (!directoryInfo.Exists)
            {
                return null;
            }

            var normalized = NormalizePath(path);
            var entry = new DriveFileSystemEntry(
                normalized,
                "",
                isDirectory: true,
                length: null,
                creationTime: directoryInfo.CreationTime,
                lastAccessTime: directoryInfo.LastAccessTime,
                lastWriteTime: directoryInfo.LastWriteTime,
                attributes: FileAttributes.Directory);
            return entry;
        }
        public IDriveFileSystemEntry GetFile(string path)
        {
            var file = new LocalFileRef(Application, NormalizePath(path), Logger);
            if (!file.Exists || file.Metadata == null)
            {
                return null;
            }

            var entry = new DriveFileSystemEntry(
                file.Path,
                file.Name,
                isDirectory: false,
                length: file.Length,
                creationTime: file.RealInfo.CreationTime,
                lastAccessTime: file.RealInfo.LastAccessTime,
                lastWriteTime: file.RealInfo.LastWriteTime,
                attributes: FileAttributes.Archive);
            return entry;
        }
        public IEnumerable<IDriveFileSystemEntry> EnumerateDirectory(string path)
        {
            var directory = new LocalFileRefDirectory(Application, NormalizePath(path), Logger);
            if (!directory.Exists)
            {
                return Enumerable.Empty<DriveFileSystemEntry>();
            }

            var directories = directory.GetDirectories()
                .Select(dir => new DriveFileSystemEntry(
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
                .Select(file => new DriveFileSystemEntry(
                    file.Path,
                    file.Name,
                    isDirectory: false,
                    length: file.Length,
                    creationTime: file.RealInfo.CreationTime,
                    lastAccessTime: file.RealInfo.LastAccessTime,
                    lastWriteTime: file.RealInfo.LastWriteTime,
                    attributes: FileAttributes.Archive));

            var response = directories.Concat(files);
            return response;
        }
        public void CreateDirectory(string path)
        {
            var directory = new LocalFileRefDirectory(Application, NormalizePath(path), Logger);
            if (!directory.Exists)
            {
                directory.Create();
            }
        }
        public void DeleteDirectory(string path)
        {
            var directory = new LocalFileRefDirectory(Application, NormalizePath(path), Logger);
            if (directory.Exists)
            {
                directory.Delete();
            }
        }
        public void MoveDirectory(string sourcePath, string destinationPath)
        {
            var directory = new LocalFileRefDirectory(Application, NormalizePath(sourcePath), Logger);
            if (!directory.Exists)
            {
                return;
            }

            directory.MoveTo(NormalizePath(destinationPath));
        }
        public void MoveFile(string sourcePath, string destinationPath)
        {
            var file = new LocalFileRef(Application, NormalizePath(sourcePath), Logger);
            if (!file.Exists)
            {
                return;
            }

            file.MoveTo(NormalizePath(destinationPath));
        }
        public Stream OpenRead(string path)
        {
            var file = new LocalFileRef(Application, NormalizePath(path), Logger);
            return file.OpenRead() ?? throw new IOException($"File {path} could not be opened for reading.");
        }
        public Stream OpenWrite(string path, FileMode mode)
        {
            var normalized = NormalizePath(path);
            var file = new LocalFileRef(Application, normalized, Logger);

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
            var file = new LocalFileRef(Application, NormalizePath(path), Logger);
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
            var rootPath = Application.RealRoot.FullName;
            var drive = new DriveInfo(Path.GetPathRoot(rootPath));
            freeBytesAvailable = drive.AvailableFreeSpace;
            totalNumberOfBytes = drive.TotalSize;
            totalNumberOfFreeBytes = drive.TotalFreeSpace;
        }

        public void Dispose()
        {
            DriveServer.Dispose();
        }
    }
}
