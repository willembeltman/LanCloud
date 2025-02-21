using LanCloud.Domain.Share;
using LanCloud.Servers.Wjp;
using System.Linq;

namespace LanCloud.Models.Dtos
{
    public class ShareDto : IWjpProxyConfig
    {
        public ShareDto() { }
        public ShareDto(LocalShare localShare)
        {
            HostName = localShare.HostName;
            Port = localShare.Port;
            ShareStripes = localShare.LocalShareStripes
                .Select(a => new ShareStripeDto(a))
                .ToArray();
        }

        public string HostName { get; set; }
        public int Port { get; set; }
        public ShareStripeDto[] ShareStripes { get; set; }
    }
}