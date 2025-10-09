using LanCloud.Domain.Application;
using LanCloud.Domain.IO;
using LanCloud.Interfaces;
using LanCloud.Repositories;

namespace LanCloud.Domain.Local;

public class LocalFile : IFile
{
    public LocalFile(LocalApplication application, string path)
    {
        Application = application;
        Path = path;
        var realFullName = PathTranslator.TranslatePathToFullName(application.RealRoot, path);
        RealInfo = new FileInfo(realFullName);
        Name = PathTranslator.TranslatePathToName(Path);
        Extention = PathTranslator.TranslatePathToExtention(Path);
    }
    public LocalFile(LocalApplication application, FileInfo realInfo)
    {
        Application = application;
        RealInfo = realInfo;
        Path = PathTranslator.TranslateFullnameToPath(application.RealRoot, realInfo);
        Name = PathTranslator.TranslatePathToName(Path);
        Extention = PathTranslator.TranslatePathToExtention(Path);
    }

    public LocalApplication Application { get; }
    public string Path { get; }
    public ILogger Logger => Application.Logger;
    public FileInfo RealInfo { get; }
    public string Name { get; }
    public string Extention { get; }

    FileMetadata? _Metadata { get; set; }
    public FileMetadata? Metadata
    {
        get
        {
            return _Metadata = _Metadata ??
                FileRefRepository.Load(RealInfo);
        }
    }

    public void SaveMetadata(FileMetadata? value)
    {
        if (value != null)
        {
            _Metadata = FileRefRepository.Save(RealInfo, value);
        }
        else
        {
            RealInfo.Delete();
            _Metadata = null;
        }
    }


    public DateTime LastWriteTime => RealInfo.LastWriteTime;
    public bool Exists => RealInfo.Exists;
    public long Length => Metadata?.Length ?? 0;
    public string Hash => Metadata?.Hash ?? string.Empty;

    public Stream Create()
    {
        var metadata = new FileMetadata(this);
        SaveMetadata(metadata);
        return new FileWriter(Application, this, Application.FileStripeBufferSize);
    }
    public Stream OpenRead()
    {
        if (Metadata == null) throw new Exception("No Metadata");
        return new FileReader(this, Application.FileStripeBufferSize);
    }
    public Stream OpenAppend()
    {
        if (Metadata == null) throw new Exception("No Metadata");
        return new FileAppender(this);
    }

    public void MoveTo(string toPath)
    {
        if (Metadata == null) return;
        if (Metadata.Stripes == null) return;
        var to = new LocalFile(Application, toPath);

        if (Extention != to.Extention)
        {
            var fileStripes = Metadata.Stripes
                .SelectMany(fileRefStripe => Application.FindFileStripes(Extention, Metadata, fileRefStripe))
                .Select(a => new
                {
                    OldFileStripe = a,
                    NewFileStripe = new LocalFileStripe(a.Info.Directory!, to.Extention, a.Indexes, a.Length!.Value, a.Hash!)
                })
                .ToArray();

            foreach (var fileStripe in fileStripes)
            {
                fileStripe.OldFileStripe.Info.MoveTo(fileStripe.NewFileStripe.Info.FullName);
            }
        }

        File.Move(RealInfo.FullName, to.RealInfo.FullName);
    }
    public void Delete()
    {
        if (Metadata == null) return;
        if (Metadata.Stripes == null) return;

        var fileStripes = Metadata.Stripes
            .SelectMany(a => Application.FindFileStripes(Extention, Metadata, a)).ToArray();

        foreach (var fileStripe in fileStripes)
        {
            fileStripe.Info.Delete();
        }

        RealInfo.Delete();
    }
}
