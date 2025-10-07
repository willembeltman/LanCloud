using LanCloud.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace LanCloud.Servers.Rpc
{
    public class RpcServer : IDisposable
    {
        private string _Status { get; set; }
        public string Status
        {
            get => _Status;
            set
            {
                _Status = value;
                Application.StatusChanged();
            }
        }

        private bool Disposed = false;
        private bool Listening = false;

        private IPEndPoint LocalEndPoint { get; }
        public IRpcHandler Handler { get; }
        public IApplication Application { get; }
        public ILogger Logger { get; }
        private TcpListener Listener { get; }
        private List<RpcClientConnection> _ActiveConnections { get; } = new List<RpcClientConnection>();

        public RpcServer(IPAddress ipAddress, int port, IRpcHandler handler, IApplication application, ILogger logger)
        {
            LocalEndPoint = new IPEndPoint(ipAddress, port);
            Handler = handler;
            Application = application;
            Logger = logger;

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

                new RpcClientConnection(this, client, Handler, Logger);
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
            lock(this)
                _ActiveConnections.Add(connection);
        }
        public void RemoveConnection(RpcClientConnection connection)
        {
            lock (this) 
                _ActiveConnections.Remove(connection);
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
}
