using LanCloudSimple.Client.Processes;
using LanCloudSimple.Shared.Engine;

namespace LanCloudSimple.Client.Services;

public class ClientService : BackgroundService
{
    private readonly ILogger<ClientService> _logger;
    private readonly IConfiguration _configuration;
    private ClientEngine? _engine;
    private ClientTcpServer? _server;

    public ClientService(ILogger<ClientService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var clientId = _configuration["ClientConfig:ClientId"] ?? "Client-" + Guid.NewGuid().ToString()[..8];
        var port = int.TryParse(_configuration["ClientConfig:Port"], out int p) ? p : 5001;
        var scanDirs = _configuration.GetSection("ClientConfig:ScanDirectories").Get<List<string>>() ?? new List<string>();

        _logger.LogInformation("Initializing LanCloudSimple Client ({clientId}) on port {port}...", clientId, port);

        _engine = new ClientEngine(scanDirs, _logger);
        _engine.Start();

        _server = new ClientTcpServer(port, clientId, _engine, _logger);
        _server.Start();

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker is stopping...");
        }
        finally
        {
            _server.Stop();
            _engine.Stop();
            _logger.LogInformation("Worker stopped.");
        }
    }
}
