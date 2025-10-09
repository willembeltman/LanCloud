using LanCloud.Domain.Application;
using LanCloud.Interfaces;
using LanCloud.Models.Dtos;

namespace LanCloud.Domain.Remote;

public class RemoteFileDirectory : IFileDirectory
{
    public RemoteFileDirectory(RemoteApplication application, FileRefDirectoryDto fileRefDirectoryDto)
    {
        Application = application;
        FileRefDirectoryDto = fileRefDirectoryDto;
    }

    public RemoteApplication Application { get; }
    public FileRefDirectoryDto FileRefDirectoryDto { get; }

    public string Path => FileRefDirectoryDto.Path;
    public bool Exists => FileRefDirectoryDto.Exists;
    public DateTime LastWriteTime => FileRefDirectoryDto.LastWriteTime;
    public string Name => PathTranslator.TranslatePathToName(Path);

    public void Create()
    {
        throw new NotImplementedException();
    }

    public void Delete()
    {
        throw new NotImplementedException();
    }

    public IFileDirectory[] GetDirectories()
    {
        throw new NotImplementedException();
    }

    public IFile[] GetFiles()
    {
        throw new NotImplementedException();
    }

    public void MoveTo(string pathTo)
    {
        throw new NotImplementedException();
    }
}
