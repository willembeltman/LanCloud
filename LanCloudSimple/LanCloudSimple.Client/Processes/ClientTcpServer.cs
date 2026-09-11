using LanCloudSimple.Shared.Engine;
using LanCloudSimple.Shared.Enums;
using LanCloudSimple.Shared.Models;

namespace LanCloudSimple.Client.Processes;

public class ClientTcpServer
{
    private readonly int _port;
    private readonly ClientEngine _engine;
    private readonly ILogger _logger;
    private readonly string _clientId;
    private System.Net.Sockets.TcpListener? _listener;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, System.Net.Sockets.NetworkStream> _controlConnections = new();
    private CancellationTokenSource? _cts;

    public ClientTcpServer(int port, string clientId, ClientEngine engine, ILogger logger)
    {
        _port = port;
        _clientId = clientId;
        _engine = engine;
        _logger = logger;

        _engine.OnFileUpdated += BroadcastFileUpdate;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, _port);
        _listener.Start();
        _logger.LogInformation("TCP Cloud Server listening on port {port}...", _port);

        _ = AcceptConnectionsAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();

        foreach (var (_, stream) in _controlConnections)
        {
            try { stream.Dispose(); } catch { }
        }
        _controlConnections.Clear();
    }

    private async Task AcceptConnectionsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await _listener!.AcceptTcpClientAsync(token);
                _ = HandleConnectionAsync(tcpClient, token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException and not ObjectDisposedException)
            {
                _logger.LogError(ex, "Error accepting TCP connection.");
            }
        }
    }

    private async Task HandleConnectionAsync(System.Net.Sockets.TcpClient tcpClient, CancellationToken token)
    {
        using (tcpClient)
        {
            var stream = tcpClient.GetStream();
            try
            {
                var reader = new StreamReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
                var headerLine = await reader.ReadLineAsync(token);
                if (string.IsNullOrEmpty(headerLine)) return;

                if (headerLine.Equals("CONTROL", StringComparison.OrdinalIgnoreCase))
                    await HandleControlConnectionAsync(stream, token);
                else if (headerLine.StartsWith("FILE:", StringComparison.OrdinalIgnoreCase))
                    await HandleFileConnectionAsync(stream, headerLine["FILE:".Length..].Trim(), token);
                else
                    _logger.LogWarning("Unknown connection header: {header}", headerLine);
            }
            catch (Exception ex) when (ex is not OperationCanceledException and not IOException)
            {
                _logger.LogError(ex, "Error handling client connection.");
            }
        }
    }

    private async Task HandleControlConnectionAsync(System.Net.Sockets.NetworkStream stream, CancellationToken token)
    {
        var connectionId = Guid.NewGuid();
        _controlConnections[connectionId] = stream;
        _logger.LogInformation("API control connection established. Id: {id}", connectionId);

        try
        {
            var handshakeMsg = new ControlMessage
            {
                Type = MessageType.HandshakeResponse,
                Payload = System.Text.Json.JsonSerializer.Serialize(new ClientInfo
                {
                    ClientId = _clientId,
                    MachineName = Environment.MachineName
                })
            };
            await LanCloudSimple.Shared.Helpers.NetworkHelper.SendJsonAsync(stream, handshakeMsg, token);

            var syncMsg = new ControlMessage
            {
                Type = MessageType.IndexSync,
                Payload = System.Text.Json.JsonSerializer.Serialize(_engine.GetIndex())
            };
            await LanCloudSimple.Shared.Helpers.NetworkHelper.SendJsonAsync(stream, syncMsg, token);

            // Keep connection alive — detect disconnection via zero-byte read
            var buffer = new byte[1024];
            while (!token.IsCancellationRequested)
            {
                int read = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (read == 0) break;
            }
        }
        finally
        {
            _controlConnections.TryRemove(connectionId, out _);
            _logger.LogInformation("API control connection closed. Id: {id}", connectionId);
        }
    }

    private async Task HandleFileConnectionAsync(System.Net.Sockets.NetworkStream stream, string requestPath, CancellationToken token)
    {
        _logger.LogInformation("File stream requested: {path}", requestPath);

        var physicalPath = _engine.ResolvePhysicalPath(requestPath);
        if (physicalPath == null)
        {
            _logger.LogWarning("File not found or path blocked: {path}", requestPath);
            stream.WriteByte(0);
            await stream.FlushAsync(token);
            return;
        }

        try
        {
            await using var fileStream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            stream.WriteByte(1);
            await stream.WriteAsync(BitConverter.GetBytes(fileStream.Length), 0, 8, token);
            await fileStream.CopyToAsync(stream, token);
            await stream.FlushAsync(token);
            _logger.LogInformation("Streamed file: {path} ({size} bytes)", requestPath, fileStream.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming file: {path}", requestPath);
        }
    }

    private void BroadcastFileUpdate(FileUpdateInfo updateInfo)
    {
        var msg = new ControlMessage
        {
            Type = MessageType.FileUpdate,
            Payload = System.Text.Json.JsonSerializer.Serialize(updateInfo)
        };

        foreach (var (_, stream) in _controlConnections)
        {
            _ = Task.Run(async () =>
            {
                try { await LanCloudSimple.Shared.Helpers.NetworkHelper.SendJsonAsync(stream, msg); }
                catch { }
            });
        }
    }
}
