namespace LanCloud.Shared.Interfaces
{
    public interface IFtpApplication
    {
        int FtpBufferSize { get; }

        void StatusChanged();
    }
}