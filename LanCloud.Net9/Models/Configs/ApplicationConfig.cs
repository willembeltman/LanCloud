namespace LanCloud.Models.Configs;

public class ApplicationConfig
{
    public string HostName { get; set; } = string.Empty;
    public string RefDirectoryName { get; set; } = string.Empty;
    public int FileStripeBufferSize { get; set; }
    public int FtpBufferMultiplier { get; set; }
    public int RpcBufferMultiplier { get; set; }
    public int VirtualDriveBufferMultiplier { get; set; }
    public LocalShareConfig[] Shares { get; set; } = [];
    public RemoteApplicationConfig[] Servers { get; set; } = [];
    public bool EnableFtpServer { get; set; }
    public bool EnableVirtualDrive { get; set; }
    public string VirtualDriveMountPoint { get; set; } = string.Empty;
    public string VirtualDriveVolumeLabel { get; set; } = string.Empty;
    public bool VirtualDriveReadOnly { get; set; }
}
