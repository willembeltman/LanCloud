namespace LanCloud.Shared.Interfaces
{
    public interface IRpcApplication
    {
        void StatusChanged();
        int FileStripeBufferSize { get; }
        int RpcBufferSize { get; }
    }
}