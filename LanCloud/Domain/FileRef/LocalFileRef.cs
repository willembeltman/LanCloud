using LanCloud.Domain.Application;
using LanCloud.Domain.FileStripe;
using LanCloud.Domain.IO.Appender;
using LanCloud.Domain.IO.Reader;
using LanCloud.Domain.IO.Writer;
using LanCloud.Models;
using LanCloud.Services;
using LanCloud.Shared.Log;
using System;
using System.IO;
using System.Linq;

namespace LanCloud.Domain.FileRef
{
    public class LocalFileRef : IFtpFile, IFileRef
    {
        public LocalFileRef(LocalApplication application, string path, ILogger logger)
        {
            Application = application;
            Path = path;
            Logger = logger;
            var realFullName = PathTranslator.TranslatePathToFullName(application.RealRoot, path);
            RealInfo = new FileInfo(realFullName);
        }
        public LocalFileRef(LocalApplication application, FileInfo realInfo, ILogger logger)
        {
            Application = application;
            RealInfo = realInfo;
            Logger = logger;
            Path = PathTranslator.TranslateFullnameToPath(application.RealRoot, realInfo);
        }

        public LocalApplication Application { get; }
        public string Path { get; }
        public ILogger Logger { get; }
        public FileInfo RealInfo { get; }

        public string Name => PathTranslator.TranslatePathToName(Path);
        public string Extention => PathTranslator.TranslatePathToExtention(Path);

        FileRefMetadata _Metadata { get; set; }
        public FileRefMetadata Metadata
        {
            get
            {
                return _Metadata = _Metadata ?? FileRefService.Load(RealInfo);
            }
            set
            {
                _Metadata = FileRefService.Save(RealInfo, value);
            }
        }

        public DateTime LastWriteTime => RealInfo.LastWriteTime;
        public bool Exists => RealInfo.Exists;
        public long? Length => Metadata?.Length;
        public string Hash => Metadata?.Hash;

        public Stream Create()
        {
            Metadata = new FileRefMetadata(this);
            return new FileRefWriter(this, Logger);
        }

        public Stream OpenRead()
        {
            if (Metadata == null) return null;
            return new FileRefReader(this, Logger);
        }

        public Stream OpenAppend()
        {
            if (Metadata == null) return null;
            return new FileRefAppender(this, Logger);
        }

        public void MoveTo(string toPath)
        {
            if (Metadata == null) return;
            var to = new LocalFileRef(Application, toPath, Logger);

            if (Extention != to.Extention)
            {
                var fileStripes = Metadata.Stripes
                    .SelectMany(fileRefStripe => Application.FindFileStripes(Extention, Metadata, fileRefStripe))
                    .Select(a => new { OldFileStripe = a, NewFileStripe = new LocalFileStripe(a.Info.Directory, to.Extention, a.Indexes, a.Length, a.Hash) })
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

            var fileStripes = Metadata.Stripes
                .SelectMany(a => Application.FindFileStripes(Extention, Metadata, a)).ToArray();

            foreach (var fileStripe in fileStripes)
            {
                fileStripe.Info.Delete();
            }

            RealInfo.Delete();
        }
    }
}
