using LanCloud.Models;
using System;

namespace LanCloud.Domain.FileRef
{
    public interface IFileRefDirectory
    {
        string Path { get; }
        bool Exists { get; }
        DateTime? LastWriteTime { get; }
        string Name { get; }

        void Create();
        void Delete();
        IFileRefDirectory[] GetDirectories();
        IFileRef[] GetFiles();
        void MoveTo(string pathTo);
    }
}