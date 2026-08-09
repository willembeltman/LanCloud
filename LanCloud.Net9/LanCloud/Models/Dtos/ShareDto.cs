using LanCloud.Domain.Share;
using LanCloud.Interfaces;

namespace LanCloud.Models.Dtos;

public class ShareDto : IRpcProxyConfig
{
    public ShareDto() { }
    public ShareDto(LocalShare localShare)
    {
        HostName = localShare.HostName;
        Port = localShare.Port;
        Indexes = localShare.Indexes;
    }

    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; }
    public int[] Indexes { get; set; } = [];
}