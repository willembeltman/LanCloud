namespace LanCloud.Interfaces
{
    public interface IApplication
    {
        int FileStripeBufferSize { get; }
        int FtpBufferSize { get; }
        int RpcBufferSize { get; }
        int VirtualDriveBufferSize { get; }
        void StatusChanged();
    }
}