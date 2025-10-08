using LanCloud.Domain.Application;
using LanCloud.Domain.FileStripe;
using LanCloud.Domain.IO.Appender;
using LanCloud.Domain.IO.Reader;
using LanCloud.Domain.IO.Writer;
using LanCloud.Interfaces;
using LanCloud.Repositories;

namespace LanCloud.Domain.FileRef;

public class LocalFileRef : IFileRef
{
    public LocalFileRef(LocalApplication application, string path)
    {
        Application = application;
        Path = path;
        var realFullName = PathTranslator.TranslatePathToFullName(application.RealRoot, path);
        RealInfo = new FileInfo(realFullName);
    }
    public LocalFileRef(LocalApplication application, FileInfo realInfo)
    {
        Application = application;
        RealInfo = realInfo;
        Path = PathTranslator.TranslateFullnameToPath(application.RealRoot, realInfo);
    }

    public LocalApplication Application { get; }
    public string Path { get; }
    public ILogger Logger => Application.Logger;
    public FileInfo RealInfo { get; }

    public string Name => PathTranslator.TranslatePathToName(Path);
    public string Extention => PathTranslator.TranslatePathToExtention(Path);

    FileRefMetadata? _Metadata { get; set; }
    public FileRefMetadata Metadata
    {
        get
        {
            return _Metadata = _Metadata ?? 
                FileRefRepository.Load(RealInfo) ?? 
                throw new Exception("Cannot load file metadata");
        }
        set
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
    }

    public DateTime LastWriteTime => RealInfo.LastWriteTime;
    public bool Exists => RealInfo.Exists;
    public long Length => Metadata!.Length;
    public string Hash => Metadata!.Hash;

    public Stream Create()
    {
        Metadata = new FileRefMetadata(this);
        return new FileRefWriter(this, Application.FileStripeBufferSize);
    }

    public Stream OpenRead()
    {
        if (Metadata == null) throw new Exception("No Metadata");
        return new FileRefReader(this, Application.FileStripeBufferSize);
    }

    public Stream OpenAppend()
    {
        if (Metadata == null) throw new Exception("No Metadata");
        return new FileRefAppender(this);
    }

    public void MoveTo(string toPath)
    {
        if (Metadata == null) return;
        if (Metadata.Stripes == null) return;
        var to = new LocalFileRef(Application, toPath);

        if (Extention != to.Extention)
        {
            var fileStripes = Metadata.Stripes
                .SelectMany(fileRefStripe => Application.FindFileStripes(Extention, Metadata, fileRefStripe))
                .Select(a => new {
                    OldFileStripe = a, 
                    NewFileStripe = new LocalFileStripe(a.Info.Directory!, to.Extention, a.Indexes, a.Length!.Value, a.Hash!) })
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
