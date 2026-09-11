namespace LanCloud.Interfaces;

public interface IRpcProxyConfig
{
    string HostName { get; set; }
    int Port { get; set; }
}