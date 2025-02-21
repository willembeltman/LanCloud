namespace LanCloud.Models.Configs
{
    public class ApplicationConfig
    {
        public string HostName { get; set; }
        public string RefDirectoryName { get; set; }
        public int FileStripeBufferSize { get; set; }
        public int WjpBufferMultiplier { get; set; }
        public LocalShareConfig[] Shares { get; set; }
        public RemoteApplicationConfig[] Servers { get; set; }
        public int FtpBufferMultiplier { get; internal set; }
    }
}
