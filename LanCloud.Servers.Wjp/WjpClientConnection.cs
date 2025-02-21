using LanCloud.Shared.Log;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace LanCloud.Servers.Wjp
{
    public class WjpClientConnection : IDisposable
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

        public WjpClientConnection(WjpServer server, TcpClient client, IWjpHandler handler, ILogger logger)
        {
            Server = server;
            Client = client;
            Handler = handler;
            Logger = logger;

            var RemoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
            Name = RemoteEndPoint.Address.ToString();

            Thread = new Thread(new ThreadStart(Start));
            Thread.Start();
        }

        public string Name { get; }
        public TcpClient Client { get; }
        public IWjpHandler Handler { get; }
        public ILogger Logger { get; }
        public WjpServer Server { get; }
        public Thread Thread { get; }

        public IWjpApplication Application => Server.Application;

        private void Start()
        {
            Server.AddConnection(this);
            Status = Logger.Info($"Starting");

            using (var stream = Client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                var requestMessageType = 0;
                var requestJson = string.Empty;
                var requestDataLength = 0;
                byte[] requestData = new byte[Server.Application.WjpBufferSize];
                var responseJson = string.Empty;
                var responseDataLength = 0;
                byte[] responseData = new byte[Server.Application.WjpBufferSize];
                try
                {
                    Status = Logger.Info($"Connected");

                    while (Client.Connected)
                    {
                        requestMessageType = reader.ReadInt32();
                        if (requestMessageType == -1)
                        {
                            writer.Write(-1);
                        }
                        else
                        {
                            requestJson = reader.ReadString();
                            requestDataLength = reader.ReadInt32();
                            if (requestDataLength >= 0)
                            {
                                reader.Read(requestData, 0, requestDataLength);
                            }
                            Handler.ProcessRequest(requestMessageType, requestJson, requestData, requestDataLength, out responseJson, responseData, out responseDataLength);
                            writer.Write(responseJson);
                            writer.Write(responseDataLength);
                            if (responseDataLength >= 0)
                            {
                                writer.Write(responseData, 0, responseDataLength);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            Dispose();
        }

        public void Dispose()
        {
            Client.Dispose();
            Server.RemoveConnection(this);
            Status = Logger.Info($"Disposed");
        }
    }
}