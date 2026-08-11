using Microsoft.Extensions.Hosting;
using StorageApi.Shared.Interfaces;

public class HostService(
    IHostApi hostApi)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return hostApi.SignalAsync("Test");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}