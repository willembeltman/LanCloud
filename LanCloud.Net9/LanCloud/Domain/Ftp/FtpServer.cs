using LanCloud.Domain.Application;
using LanCloud.Domain.Rpc;
using LanCloud.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace LanCloud.Domain.Ftp;

public class FtpServer : StatusBase, IDisposable
{
    private bool Disposed = false;
    private bool Listening = false;

    private List<ClientConnection> ActiveConnections;

    private IPEndPoint LocalEndPoint { get; }
    private TcpListener Listener { get; }

    public FtpServer(LocalApplication application, IPAddress ipAddress, int port) : base(application)
    {
        LocalEndPoint = new IPEndPoint(ipAddress, port);
        Listener = new TcpListener(LocalEndPoint);

        Listening = true;
        Listener.Start();

        ActiveConnections = new List<ClientConnection>();

        Listener.BeginAcceptTcpClient(HandleAcceptTcpClient, Listener);

        Status = Logger.Info("OK");
    }

    private void HandleAcceptTcpClient(IAsyncResult result)
    {
        if (Listening)
        {
            Listener.BeginAcceptTcpClient(HandleAcceptTcpClient, Listener);

            TcpClient client = Listener.EndAcceptTcpClient(result);

            ClientConnection connection = new ClientConnection(client, Application);

            ActiveConnections.Add(connection);

            ThreadPool.QueueUserWorkItem(connection.HandleClient, client);
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
                Logger.Info("Stopping FtpServer");

                Listening = false;
                Listener.Stop();

                foreach (ClientConnection conn in ActiveConnections)
                {
                    conn.Dispose();
                }
            }
        }

        Disposed = true;
    }
}
