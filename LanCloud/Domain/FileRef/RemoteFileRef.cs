using LanCloud.Domain.Application;
using System;
using System.IO;

namespace LanCloud.Domain.FileRef
{
    public class RemoteFileRef : IFileRef
    {
        public RemoteFileRef(RemoteApplication application, FileRefDto fileRefDto)
        {
            Application = application;
            FileRefDto = fileRefDto;
        }

        public RemoteApplication Application { get; }
        public FileRefDto FileRefDto { get; }

        public string Path => FileRefDto.Path;
        public bool Exists => FileRefDto.Exists;
        public long? Length => FileRefDto.Length;
        public string Hash => FileRefDto.Hash;
        public DateTime LastWriteTime => FileRefDto.LastWriteTime;
        public string Name => PathTranslator.TranslatePathToName(Path);
        public string Extention => PathTranslator.TranslatePathToExtention(Path);

        public Stream Create()
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }

        public void MoveTo(string toPath)
        {
            throw new NotImplementedException();
        }

        public Stream OpenAppend()
        {
            throw new NotImplementedException();
        }

        public Stream OpenRead()
        {
            throw new NotImplementedException();
        }
    }
}
