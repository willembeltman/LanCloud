using LanCloud.Domain.Application;
using LanCloud.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace LanCloud.Domain.Rpc;

public class RpcServer : StatusBase, IDisposable
{
    private bool Disposed = false;
    private bool Listening = false;

    private IPEndPoint LocalEndPoint { get; }
    public IRpcHandler Handler { get; }
    private TcpListener Listener { get; }
    private List<RpcClientConnection> _ActiveConnections { get; } = new List<RpcClientConnection>();

    public RpcServer(IRpcHandler handler, LocalApplication application, IPAddress ipAddress, int port) : base(application)
    {
        Handler = handler;
        LocalEndPoint = new IPEndPoint(ipAddress, port);

        Listener = new TcpListener(LocalEndPoint);

        Listening = true;
        Listener.Start();

        Listener.BeginAcceptTcpClient(HandleAcceptTcpClient, Listener);

        Status = Logger.Info($"OK");
    }

    private void HandleAcceptTcpClient(IAsyncResult result)
    {
        if (Listening)
        {
            Listener.BeginAcceptTcpClient(HandleAcceptTcpClient, Listener);

            var client = Listener.EndAcceptTcpClient(result);

            new RpcClientConnection(this, client, Handler);
        }
    }
    public RpcClientConnection[] GetActiveConnections()
    {
        RpcClientConnection[] res;
        lock (this)
            res = _ActiveConnections.ToArray();
        return res;
    }
    public void AddConnection(RpcClientConnection connection)
    {
        lock (this)
        {
            _ActiveConnections.Add(connection);
        }
    }
    public void RemoveConnection(RpcClientConnection connection)
    {
        lock (this)
        {
            _ActiveConnections.Remove(connection);
            connection.Dispose();
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
                Listening = false;
                Listener.Stop();

                var conns = _ActiveConnections.ToArray();

                foreach (RpcClientConnection conn in conns)
                {
                    conn.Dispose();
                }
            }
        }

        Disposed = true;
    }
}
