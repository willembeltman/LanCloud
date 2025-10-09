using LanCloud.Domain.Application;
using LanCloud.Interfaces;

namespace LanCloud.Domain.Rpc;

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

    public ILogger Logger => Application.Logger;
}