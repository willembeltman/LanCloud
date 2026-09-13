using System.Net;
using System.Net.Sockets;

namespace LanCloud.Api.Services;

public class FtpServer(IServiceProvider serviceProvider) : IHostedService, IDisposable
{
    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        LocalEndPoint = new IPEndPoint(IPAddress.Any, 21);
        Listener = new TcpListener(LocalEndPoint);

        Listening = true;
        Listener.Start();

        ActiveConnections = new List<FtpConnection>();

        Listener.BeginAcceptTcpClient(HandleAcceptTcpClient, Listener);
    }

    async Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
    }

    private bool Disposed = false;
    private bool Listening = false;

    private List<FtpConnection> ActiveConnections = [];

    private IPEndPoint? LocalEndPoint;
    private TcpListener? Listener;

    private void HandleAcceptTcpClient(IAsyncResult result)
    {
        if (Listening  && Listener != null)
        {
            Listener.BeginAcceptTcpClient(HandleAcceptTcpClient, Listener);

            TcpClient client = Listener.EndAcceptTcpClient(result);
            var scope = serviceProvider.CreateAsyncScope();
            var fileSystem = scope.ServiceProvider.GetRequiredService<FileSystem>();
            FtpConnection connection = new FtpConnection(fileSystem, client);

            ActiveConnections.Add(connection);

            _ = Task.Run(async () => { await connection.HandleClient(); });
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            if (disposing)
            {
                //Logger.Info("Stopping FtpServer");

                Listening = false;
                Listener.Stop();

                foreach (FtpConnection conn in ActiveConnections)
                {
                    conn.Dispose();
                }
            }
        }

        Disposed = true;
    }

}
