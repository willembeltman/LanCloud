using System;
using System.IO;

namespace LanCloud.Shared.Interfaces
{
    public interface IFtpFile
    {
        string Path { get; }
        string Name { get; }
        long? Length { get; }
        DateTime LastWriteTime { get; }
        string Extention { get; }
        bool Exists { get; }

        Stream Create();
        void Delete();
        Stream OpenAppend();
        Stream OpenRead();
        void MoveTo(string toPath);
    }
}