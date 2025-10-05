using System;
using System.IO;

namespace LanCloud.Shared.Interfaces
{
    public interface ILanCloudFileSystemEntry
    {
        FileAttributes Attributes { get; }
        DateTime? CreationTime { get; }
        bool IsDirectory { get; }
        DateTime? LastAccessTime { get; }
        DateTime? LastWriteTime { get; }
        long? Length { get; }
        string Name { get; }
        string Path { get; }
    }
}