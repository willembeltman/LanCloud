using LanCloud.Domain.Application;

namespace LanCloud.Servers.Rpc;

public class StatusBase(LocalApplication application)
{
    public LocalApplication Application { get; } = application;

    private string? _Status { get; set; }
    public string Status
    {
        get => _Status ?? string.Empty;
        set
        {
            _Status = value;
            Application.StatusChanged();
        }
    }
}