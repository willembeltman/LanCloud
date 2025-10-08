using LanCloud.Interfaces;

namespace LanCloud.Models.Config
{
    public class RemoteApplicationConfig : IRpcProxyConfig
    {
        public string? HostName { get; set; }
        public int Port { get; set; }
        public bool IsThisComputer { get; set; }
    }
}