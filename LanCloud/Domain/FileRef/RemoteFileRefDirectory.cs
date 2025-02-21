using LanCloud.Domain.Application;
using LanCloud.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanCloud.Domain.FileRef
{
    public class RemoteFileRefDirectory : IFileRefDirectory
    {
        public RemoteFileRefDirectory(RemoteApplication application, FileRefDirectoryDto fileRefDirectoryDto)
        {
            Application = application;
            FileRefDirectoryDto = fileRefDirectoryDto;
        }

        public RemoteApplication Application { get; }
        public FileRefDirectoryDto FileRefDirectoryDto { get; }

        public string Path => FileRefDirectoryDto.Path;
        public bool Exists => FileRefDirectoryDto.Exists;
        public DateTime? LastWriteTime => FileRefDirectoryDto.LastWriteTime;

        public string Name => PathTranslator.TranslatePathToName(Path);

        public void Create()
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }

        public IFileRefDirectory[] GetDirectories()
        {
            throw new NotImplementedException();
        }

        public IFileRef[] GetFiles()
        {
            throw new NotImplementedException();
        }

        public void MoveTo(string pathTo)
        {
            throw new NotImplementedException();
        }
    }
}
