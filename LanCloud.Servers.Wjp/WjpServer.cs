using LanCloud.Shared.Log;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace LanCloud.Servers.Wjp
{
    public class WjpServer : IDisposable
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
        public IWjpHandler Handler { get; }
        public IWjpApplication Application { get; }
        public ILogger Logger { get; }
        private TcpListener Listener { get; }
        private List<WjpClientConnection> _ActiveConnections { get; } = new List<WjpClientConnection>();

        public WjpServer(IPAddress ipAddress, int port, IWjpHandler handler, IWjpApplication application, ILogger logger)
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

                new WjpClientConnection(this, client, Handler, Logger);
            }
        }
        public WjpClientConnection[] GetActiveConnections()
        {
            WjpClientConnection[] res;
            lock (this)
                res = _ActiveConnections.ToArray();
            return res;
        }
        public void AddConnection(WjpClientConnection connection)
        {
            lock(this)
                _ActiveConnections.Add(connection);
        }
        public void RemoveConnection(WjpClientConnection connection)
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

                    foreach (WjpClientConnection conn in conns)
                    {
                        conn.Dispose();
                    }
                }
            }

            Disposed = true;
        }
    }
}
