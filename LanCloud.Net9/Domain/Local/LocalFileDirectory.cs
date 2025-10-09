using LanCloud.Domain.Application;
using LanCloud.Interfaces;

namespace LanCloud.Domain.Local;

public class LocalFileDirectory : IFileDirectory
{
    public LocalFileDirectory(LocalApplication application, string path)
    {
        Application = application;
        Path = path;
        var realFullName = PathTranslator.TranslateDirectoryPathToFullName(application.RealRoot, path);
        RealInfo = new DirectoryInfo(realFullName);
    }
    public LocalFileDirectory(LocalApplication application, DirectoryInfo realInfo)
    {
        Application = application;
        RealInfo = realInfo;
        Path = PathTranslator.TranslateDirectoryFullNameToPath(application.RealRoot, realInfo);
    }

    public LocalApplication Application { get; }
    public string Path { get; }
    private DirectoryInfo RealInfo { get; }

    public bool Exists => RealInfo.Exists;
    public string Name => RealInfo.Name;
    public DateTime LastWriteTime => RealInfo.LastWriteTime;

    public void Create() => RealInfo.Create();
    public void MoveTo(string pathTo)
    {
        var to = PathTranslator.TranslatePathToFullName(Application.RealRoot, pathTo);
        RealInfo.MoveTo(to);
    }
    public void Delete()
    {
        RealInfo.Delete(false);
    }
    public LocalFileDirectory[] GetDirectories()
        => RealInfo
            .GetDirectories()
            .Select(dirinfo => new LocalFileDirectory(Application, dirinfo))
            .ToArray();
    public LocalFile[] GetFiles()
        => RealInfo
            .GetFiles("*.fileref")
            .Select(realInfo => new LocalFile(Application, realInfo))
            .ToArray();
    
    IFileDirectory[] IFileDirectory.GetDirectories() => GetDirectories();
    IFile[] IFileDirectory.GetFiles() => GetFiles();
}
