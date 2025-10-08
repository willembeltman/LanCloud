using LanCloud.Domain.Application;
using LanCloud.Servers.Ftp;
using System.Net;

namespace LanCloud.Domain.VirtualFtp;

public class VirtualFtpServer : IDisposable
{
    private readonly LocalApplication Application;

    public VirtualFtpServer(LocalApplication application)
    {
        Application = application;
        FtpServer = new FtpServer(application, IPAddress.Any, 21);

        application.Logger.Info($"OK");
    }

    public FtpServer FtpServer { get; }

    public string HostName => Application.HostName;
    public int Port => 21;

    public void Dispose()
    {
        FtpServer.Dispose();
    }
}
