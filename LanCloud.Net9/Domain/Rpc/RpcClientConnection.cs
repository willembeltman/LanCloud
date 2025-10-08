using LanCloud.Domain.Application;
using LanCloud.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace LanCloud.Domain.Rpc;

public class RpcClientConnection : StatusBase, IDisposable
{
    public RpcClientConnection(RpcServer server, TcpClient client) : base(server.Application)
    {
        Server = server;
        Client = client;

        var RemoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
        Name = RemoteEndPoint?.Address.ToString() ?? string.Empty;

        Thread = new Thread(new ThreadStart(Start));
        Thread.Start();
    }

    public string Name { get; }
    public TcpClient Client { get; }
    public RpcServer Server { get; }
    public Thread Thread { get; }


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
            byte[] requestData = new byte[Server.Application.RpcBufferSize];
            var responseJson = string.Empty;
            var responseDataLength = 0;
            byte[] responseData = new byte[Server.Application.RpcBufferSize];
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
                        Server.Handler.ProcessRequest(requestMessageType, requestJson, requestData, requestDataLength, out responseJson, responseData, out responseDataLength);
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