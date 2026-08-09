using LanCloud.Domain.Application;
using LanCloud.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace LanCloud.Domain.Rpc;

public class RpcClientConnection : StatusBase, IDisposable
{
    private readonly TcpClient Client;
    private readonly IRpcHandler Handler;
    private readonly RpcServer Server;
    private readonly Thread Thread;
    private bool KillSwitch;

    public RpcClientConnection(RpcServer server, TcpClient client, IRpcHandler handler) : base(server.Application)
    {
        Server = server;
        Client = client;
        Handler = handler;

        var RemoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
        Name = RemoteEndPoint?.Address.ToString() ?? string.Empty;

        Thread = new Thread(new ThreadStart(Kernel));
        Thread.Start();
    }
    public string Name { get; }

    private void Kernel()
    {
        Server.AddConnection(this);

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
                Status = Logger.Info($"OK");

                while (Client.Connected && !KillSwitch)
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
                        if (responseJson == null)
                        {
                            writer.Write(true);
                        }
                        else
                        {
                            writer.Write(false);
                            writer.Write(responseJson);
                        }
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

        Server.RemoveConnection(this);
    }

    public void Dispose()
    {
        KillSwitch = true;
        Client.Dispose();
    }
}