using LanCloud.Domain.Application;
using LanCloud.Models;
using LanCloud.Shared.Log;
using System;
using System.IO;
using System.Linq;

namespace LanCloud.Domain.FileRef
{
    public class LocalFileRefDirectory : IFtpDirectory, IFileRefDirectory
    {
        public LocalFileRefDirectory(LocalApplication application, string path, ILogger logger)
        {
            Application = application;
            Path = path;
            Logger = logger;
            var realFullName = PathTranslator.TranslateDirectoryPathToFullName(application.RealRoot, path);
            RealInfo = new DirectoryInfo(realFullName);
        }
        public LocalFileRefDirectory(LocalApplication application, DirectoryInfo realInfo, ILogger logger)
        {
            Application = application;
            RealInfo = realInfo;
            Logger = logger;
            Path = PathTranslator.TranslateDirectoryFullNameToPath(application.RealRoot, realInfo);
        }

        public LocalApplication Application { get; }
        public string Path { get; }
        private DirectoryInfo RealInfo { get; }
        public ILogger Logger { get; }

        public string Name => RealInfo.Name;
        public DateTime? LastWriteTime => RealInfo.Exists ? RealInfo.LastWriteTime : (DateTime?)null;
        public bool Exists => RealInfo.Exists;

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
        public LocalFileRefDirectory[] GetDirectories()
            => RealInfo
                .GetDirectories()
                .Select(dirinfo => new LocalFileRefDirectory(Application, dirinfo, Logger))
                .ToArray();
        public LocalFileRef[] GetFiles()
            => RealInfo
                .GetFiles("*.fileref")
                .Select(realInfo => new LocalFileRef(Application, realInfo, Logger))
                .ToArray();

        IFtpDirectory[] IFtpDirectory.GetDirectories() => GetDirectories();
        IFileRefDirectory[] IFileRefDirectory.GetDirectories() => GetDirectories();

        IFtpFile[] IFtpDirectory.GetFiles() => GetFiles();
        IFileRef[] IFileRefDirectory.GetFiles() => GetFiles();
    }
}
