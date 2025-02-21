namespace LanCloud.Servers.Wjp
{
    public interface IWjpApplication
    {
        void StatusChanged();
        int FileStripeBufferSize { get; }
        int WjpBufferSize { get; }
    }
}