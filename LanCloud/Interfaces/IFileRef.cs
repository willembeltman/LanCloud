using System;
using System.IO;

namespace LanCloud.Domain.FileRef
{
    public interface IFileRef
    {
        bool Exists { get; }
        string Extention { get; }
        long? Length { get; }
        string Name { get; }
        string Path { get; }
        string Hash { get; }
        DateTime LastWriteTime { get; }

        Stream Create();
        void Delete();
        void MoveTo(string toPath);
        Stream OpenAppend();
        Stream OpenRead();
    }
}