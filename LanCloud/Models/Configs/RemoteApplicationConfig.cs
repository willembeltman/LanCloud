using LanCloud.Servers.Wjp;

namespace LanCloud.Models.Configs
{
    public class RemoteApplicationConfig : IWjpProxyConfig
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        public bool IsThisComputer { get; set; }
    }
}