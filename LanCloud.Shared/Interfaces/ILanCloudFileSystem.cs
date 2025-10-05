using System;
using System.Collections.Generic;
using System.IO;

namespace LanCloud.Shared.Interfaces
{
    public interface ILanCloudFileSystem
    {
        bool DirectoryExists(string path);
        bool FileExists(string path);
        ILanCloudFileSystemEntry GetDirectory(string path);
        ILanCloudFileSystemEntry GetFile(string path);
        IEnumerable<ILanCloudFileSystemEntry> EnumerateDirectory(string path);
        void CreateDirectory(string path);
        void DeleteDirectory(string path);
        void MoveDirectory(string sourcePath, string destinationPath);
        void MoveFile(string sourcePath, string destinationPath);
        Stream OpenRead(string path);
        Stream OpenWrite(string path, FileMode mode);
        void DeleteFile(string path);
        void SetDirectoryTimestamps(string path, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime);
        void SetFileTimestamps(string path, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime);
        void GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes);
    }
}
