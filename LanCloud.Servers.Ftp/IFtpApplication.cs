namespace LanCloud.Servers.Ftp
{
    public interface IFtpApplication
    {
        int FtpBufferSize { get; }

        void StatusChanged();
    }
}