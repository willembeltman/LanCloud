using LanCloud.Domain.Application;
using LanCloud.Domain.FileRef;
using LanCloud.Models;
using LanCloud.Servers.Ftp;
using LanCloud.Servers.Ftp.Interfaces;
using LanCloud.Shared.Log;
using System;
using System.IO;
using System.Net;

namespace LanCloud.Domain.VirtualFtp
{
    public class VirtualFtpServer : IFtpHandler, IDisposable
    {
        public VirtualFtpServer(LocalApplication application, ILogger logger)
        {
            Application = application;
            Logger = logger;

            FtpServer = new FtpServer(IPAddress.Any, 21, this, application, logger);

            Logger.Info($"Loaded");
        }

        public LocalApplication Application { get; }
        public ILogger Logger { get; }
        public FtpServer FtpServer { get; }

        public string HostName => Application.HostName;
        public int Port => 21;

        public IFtpUser ValidateUser(string userName, string password)
            => Application.Authentication.ValidateUser(userName, password);

        public IFtpDirectory[] EnumerateDirectories(string path)
            => new LocalFileRefDirectory(Application, path, Logger).GetDirectories();
        public IFtpFile[] EnumerateFiles(string path)
            => new LocalFileRefDirectory(Application, path, Logger).GetFiles();

        public void CreateDirectory(string path)
            => new LocalFileRefDirectory(Application, path, Logger).Create();
        public void DeleteDirectory(string path)
            => new LocalFileRefDirectory(Application, path, Logger).Delete();
        public bool DirectoryExists(string path)
            => new LocalFileRefDirectory(Application, path, Logger).Exists;
        public void DirectoryMove(string renameFrom, string renameTo)
        {
            var from = new LocalFileRefDirectory(Application, renameFrom, Logger);
            from.MoveTo(renameTo);
        }

        public bool FileExists(string path)
            => new LocalFileRef(Application, path, Logger).Exists;
        public void FileDelete(string path)
            => new LocalFileRef(Application, path, Logger).Delete();
        public void FileMove(string renameFrom, string renameTo)
        {
            var from = new LocalFileRef(Application, renameFrom, Logger);
            from.MoveTo(renameTo);
        }
        public DateTime FileGetLastWriteTime(string path)
            => new LocalFileRef(Application, path, Logger).LastWriteTime;

        public Stream FileOpenRead(string path)
            => new LocalFileRef(Application, path, Logger).OpenRead();
        public Stream FileOpenWriteCreate(string path)
            => new LocalFileRef(Application, path, Logger).Create();
        public Stream FileOpenWriteAppend(string path)
            => new LocalFileRef(Application, path, Logger).OpenAppend();

        public void Dispose()
        {
            FtpServer.Dispose();
        }
    }
}