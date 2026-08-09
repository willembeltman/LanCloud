using LanCloudSimple.Api.Helpers;
using LanCloudSimple.Api.Models;
using LanCloudSimple.Shared.Engine;
using LanCloudSimple.Shared.Enums;
using LanCloudSimple.Shared.Helpers;
using LanCloudSimple.Shared.Models;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace LanCloudSimple.Api.Services;

public class CloudService : IHostedService
{
    private readonly ILogger<CloudService> _logger;
    private readonly IConfiguration _configuration;

    private readonly List<string> _clientAddresses;
    private readonly string _localStoragePath;
    public string LocalStoragePath => _localStoragePath;

    // In-memory index: Key = ClientId (or "Local"), Value = path -> file DTO
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CloudFileDto>> _clientFiles
        = new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, ClientInfo> _clients
        = new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, string> _clientIdToAddress
        = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<Task> _connectionTasks = new();
    private CancellationTokenSource? _cts;

    // The local storage is indexed and watched via a CloudEngine instance (same as clients use)
    private ClientEngine? _localEngine;

    public CloudService(ILogger<CloudService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        _clientAddresses = _configuration.GetSection("ClientConfig:Clients").Get<List<string>>() ?? new List<string>();
        _localStoragePath = _configuration["ClientConfig:LocalStoragePath"] ?? Path.Combine(AppContext.BaseDirectory, "LocalStorage");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting CloudService...");
        _cts = new CancellationTokenSource();

        if (!Directory.Exists(_localStoragePath))
            Directory.CreateDirectory(_localStoragePath);

        // Index and watch local storage using CloudEngine (same logic as the client)
        _localEngine = new ClientEngine(new List<string> { _localStoragePath }, _logger);
        _localEngine.OnFileUpdated += OnLocalFileUpdated;
        _localEngine.Start();

        // Populate _clientFiles["Local"] from the engine's initial index
        RefreshLocalIndex();

        // Connect to each remote client in the background
        foreach (var addr in _clientAddresses)
            _connectionTasks.Add(Task.Run(() => ConnectToClientLoopAsync(addr, _cts.Token)));

        await Task.CompletedTask;
    }
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping CloudService...");
        _cts?.Cancel();

        _localEngine?.Stop();

        try { await Task.WhenAll(_connectionTasks); }
        catch { }
    }

    // Local Storage
    private void RefreshLocalIndex()
    {
        if (_localEngine == null) return;

        var localFiles = new ConcurrentDictionary<string, CloudFileDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in _localEngine.GetIndex())
            localFiles[file.Path] = file;

        _clientFiles["Local"] = localFiles;
        _clients["Local"] = new ClientInfo { ClientId = "Local", MachineName = Environment.MachineName };
    }
    private void OnLocalFileUpdated(FileUpdateInfo update)
    {
        var localDict = _clientFiles.GetOrAdd("Local", _ => new ConcurrentDictionary<string, CloudFileDto>(StringComparer.OrdinalIgnoreCase));

        if (update.UpdateType == FileUpdateType.Deleted)
        {
            localDict.TryRemove(update.File.Path, out _);
            _logger.LogInformation("Local storage file removed from index: {path}", update.File.Path);
        }
        else
        {
            localDict[update.File.Path] = update.File;
            _logger.LogInformation("Local storage file {action}: {path}", update.UpdateType, update.File.Path);
        }
    }

    // Remote Client Connections
    private async Task ConnectToClientLoopAsync(string address, CancellationToken token)
    {
        var parts = address.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[1], out int port))
        {
            _logger.LogError("Invalid client address (expected host:port): {address}", address);
            return;
        }
        var host = parts[0];

        while (!token.IsCancellationRequested)
        {
            _logger.LogInformation("Attempting to connect to client {host}:{port}...", host, port);
            try
            {
                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(host, port, token);
                using var stream = tcpClient.GetStream();

                var controlLineBytes = Encoding.UTF8.GetBytes("CONTROL\n");
                await stream.WriteAsync(controlLineBytes, 0, controlLineBytes.Length, token);
                await stream.FlushAsync(token);

                string? currentClientId = null;

                while (!token.IsCancellationRequested)
                {
                    var msg = await NetworkHelper.ReceiveJsonAsync<ControlMessage>(stream, token);
                    if (msg == null) break;

                    switch (msg.Type)
                    {
                        case MessageType.HandshakeResponse:
                            var info = JsonSerializer.Deserialize<ClientInfo>(msg.Payload ?? "{}");
                            if (info != null && !string.IsNullOrEmpty(info.ClientId))
                            {
                                currentClientId = info.ClientId;
                                _clients[currentClientId] = info;
                                _clientIdToAddress[currentClientId] = address;
                                _logger.LogInformation("Connected to client: {id} on {machine}", info.ClientId, info.MachineName);
                            }
                            break;

                        case MessageType.IndexSync when currentClientId != null:
                            var files = JsonSerializer.Deserialize<List<CloudFileDto>>(msg.Payload ?? "[]") ?? new();
                            var dict = new ConcurrentDictionary<string, CloudFileDto>(StringComparer.OrdinalIgnoreCase);
                            foreach (var f in files)
                                dict[f.Path] = f;
                            _clientFiles[currentClientId] = dict;
                            _logger.LogInformation("Synced index for client {id}: {count} files", currentClientId, files.Count);
                            break;

                        case MessageType.FileUpdate when currentClientId != null:
                            var update = JsonSerializer.Deserialize<FileUpdateInfo>(msg.Payload ?? "{}");
                            if (update?.File != null)
                            {
                                var clientDict = _clientFiles.GetOrAdd(currentClientId, _ => new ConcurrentDictionary<string, CloudFileDto>(StringComparer.OrdinalIgnoreCase));
                                if (update.UpdateType == FileUpdateType.Deleted)
                                    clientDict.TryRemove(update.File.Path, out _);
                                else
                                    clientDict[update.File.Path] = update.File;
                                _logger.LogInformation("Real-time update from {id}: {action} {path}", currentClientId, update.UpdateType, update.File.Path);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning("Disconnected from {host}:{port}: {msg}", host, port, ex.Message);
            }

            await Task.Delay(5000, token);
        }
    }
    public async Task<(Stream Stream, long Length)?> GetFileStreamAsync(string clientId, string clientPath)
    {
        if (clientId.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            var physicalPath = _localEngine?.ResolvePhysicalPath(clientPath);
            if (physicalPath == null || !File.Exists(physicalPath))
                return null;

            var fs = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            return (fs, fs.Length);
        }

        if (!_clientIdToAddress.TryGetValue(clientId, out var address))
            return null;

        var parts = address.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[1], out int port))
            return null;

        var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(parts[0], port);

        var stream = tcpClient.GetStream();
        var header = Encoding.UTF8.GetBytes($"FILE:{clientPath}\n");
        await stream.WriteAsync(header, 0, header.Length);
        await stream.FlushAsync();

        int status = stream.ReadByte();
        if (status != 1)
        {
            tcpClient.Close();
            return null;
        }

        byte[] lenBytes = new byte[8];
        int read = 0;
        while (read < 8)
        {
            int r = await stream.ReadAsync(lenBytes, read, 8 - read);
            if (r == 0) { tcpClient.Close(); return null; }
            read += r;
        }

        return (new NetworkStreamWithCleanup(tcpClient, stream), BitConverter.ToInt64(lenBytes, 0));
    }
    public List<BrowseNode> Browse(string relativePath)
    {
        var path = (relativePath ?? "").Replace('\\', '/').Trim('/');
        var result = new List<BrowseNode>();
        var seenDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (clientId, filesDict) in _clientFiles)
        {
            foreach (var (clientFullPath, file) in filesDict)
            {
                var mergedPath = StripRootName(clientFullPath);
                if (mergedPath == null) continue;

                if (path == "")
                {
                    var segments = mergedPath.Split('/');
                    if (segments.Length == 1)
                    {
                        result.Add(new BrowseNode
                        {
                            Name = segments[0],
                            Path = segments[0],
                            IsDirectory = false,
                            Size = file.Size,
                            MediaDate = file.MediaDate,
                            ClientId = clientId
                        });
                    }
                    else if (seenDirs.Add(segments[0]))
                    {
                        result.Add(new BrowseNode { Name = segments[0], Path = segments[0], IsDirectory = true });
                    }
                }
                else if (mergedPath.StartsWith(path + "/", StringComparison.OrdinalIgnoreCase))
                {
                    var rest = mergedPath[(path.Length + 1)..];
                    var segments = rest.Split('/');
                    if (segments.Length == 1)
                    {
                        result.Add(new BrowseNode
                        {
                            Name = segments[0],
                            Path = path + "/" + segments[0],
                            IsDirectory = false,
                            Size = file.Size,
                            MediaDate = file.MediaDate,
                            ClientId = clientId
                        });
                    }
                    else
                    {
                        var subDirPath = path + "/" + segments[0];
                        if (seenDirs.Add(subDirPath))
                            result.Add(new BrowseNode { Name = segments[0], Path = subDirPath, IsDirectory = true });
                    }
                }
            }
        }

        return result.OrderBy(x => !x.IsDirectory).ThenBy(x => x.Name).ToList();
    }
    public (string ClientId, string ClientPath)? ResolveMergedPath(string mergedPath)
    {
        var path = (mergedPath ?? "").Replace('\\', '/').Trim('/');

        foreach (var (clientId, filesDict) in _clientFiles)
        {
            foreach (var (clientFullPath, _) in filesDict)
            {
                var candidate = StripRootName(clientFullPath);
                if (candidate != null && candidate.Equals(path, StringComparison.OrdinalIgnoreCase))
                    return (clientId, clientFullPath);
            }
        }
        return null;
    }
    private static string? StripRootName(string path)
    {
        var index = path.IndexOf('/');
        return index == -1 ? null : path[(index + 1)..];
    }

    // Upload / Delete / Create Directory
    public async Task SaveFileAsync(string relativePath, Stream dataStream, CancellationToken cancellationToken)
    {
        var targetFullPath = Path.GetFullPath(Path.Combine(_localStoragePath, relativePath));
        var rootDirFull = Path.GetFullPath(_localStoragePath);

        if (!targetFullPath.StartsWith(rootDirFull, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Path traversal attempt detected.");

        var dir = Path.GetDirectoryName(targetFullPath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        await using var fs = new FileStream(targetFullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await dataStream.CopyToAsync(fs, cancellationToken);
    }
    public void DeleteLocalPath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_localStoragePath, relativePath));
        var rootDirFull = Path.GetFullPath(_localStoragePath);

        if (!fullPath.StartsWith(rootDirFull, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Path traversal attempt detected.");

        if (File.Exists(fullPath))
            File.Delete(fullPath);
        else if (Directory.Exists(fullPath))
            Directory.Delete(fullPath, recursive: true);
        else
            throw new FileNotFoundException("Path not found.", fullPath);
    }
    public void CreateLocalDirectory(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_localStoragePath, relativePath));
        var rootDirFull = Path.GetFullPath(_localStoragePath);

        if (!fullPath.StartsWith(rootDirFull, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Path traversal attempt detected.");

        Directory.CreateDirectory(fullPath);
    }
}
