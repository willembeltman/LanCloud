using LanCloud.Api.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace LanCloud.Api.Ftp;

public class FtpServer(
    LanCloudApiConfig apiConfig,
    IServiceProvider serviceProvider,
    ILoggerFactory loggerFactory)
    : IHostedService
{
    readonly ILogger<FtpServer> Logger = loggerFactory.CreateLogger<FtpServer>();
    readonly List<FtpConnection> ActiveConnections = [];
    readonly CancellationTokenSource Cts = new();

    bool Listening = false;

    IPEndPoint? LocalEndPoint;
    TcpListener? Listener;

    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        if (Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation("Stopping FtpServer");

        LocalEndPoint = new IPEndPoint(IPAddress.Any, 21);
        Listener = new TcpListener(LocalEndPoint);

        Listening = true;
        Listener.Start();

        Listener.BeginAcceptTcpClient(HandleAcceptTcpClient, Listener);
    }

    async Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        if (Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation("Stopping FtpServer");

        Listening = false;
        Listener?.Stop();
        Listener?.Dispose();

        await Cts.CancelAsync();
        Cts.Dispose();
    }

    private void HandleAcceptTcpClient(IAsyncResult result)
    {
        if (Listening && Listener != null)
        {
            if (Logger.IsEnabled(LogLevel.Information))
                Logger.LogInformation("New FTP connection accepted");

            Listener.BeginAcceptTcpClient(HandleAcceptTcpClient, Listener);

            var client = Listener.EndAcceptTcpClient(result);
            var scope = serviceProvider.CreateAsyncScope();
            var fileSystem = scope.ServiceProvider.GetRequiredService<IFileSystemDirect>();
            var connection = new FtpConnection(scope, loggerFactory, fileSystem, client, apiConfig.CertificateFilename);

            ActiveConnections.Add(connection);

            _ = Task.Run(async () => { await connection.Start(Cts.Token); });
        }
    }
}
