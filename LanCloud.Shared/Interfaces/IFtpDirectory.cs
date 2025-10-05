using System;

namespace LanCloud.Shared.Interfaces
{
    public interface IFtpDirectory
    {
        string Path { get; }
        string Name { get; }
        DateTime? LastWriteTime { get; }
        bool Exists { get; }

        void Create();
        void Delete();
        IFtpDirectory[] GetDirectories();
        IFtpFile[] GetFiles();
        void MoveTo(string pathTo);
    }
}