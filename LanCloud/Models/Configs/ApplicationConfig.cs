namespace LanCloud.Models.Configs
{
    public class ApplicationConfig
    {
        public string HostName { get; set; }
        public string RefDirectoryName { get; set; }
        public int FileStripeBufferSize { get; set; }
        public int RpcBufferMultiplier { get; set; }
        public LocalShareConfig[] Shares { get; set; }
        public RemoteApplicationConfig[] Servers { get; set; }
        public int FtpBufferMultiplier { get; set; }
        public bool EnableFtpServer { get; set; }
        public bool EnableVirtualDrive { get; set; }
        public string VirtualDriveMountPoint { get; set; }
        public string VirtualDriveVolumeLabel { get; set; }
        public bool VirtualDriveReadOnly { get; set; }
    }
}
