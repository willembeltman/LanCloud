namespace LanCloud.Interfaces;

public interface IShare
{
    IFileStripe? FindFileStripe(string extention, string hash, long length, int[] indexes);
    IFileStripe CreateFileStripeSession(string extention);
    void AddFileStripe(IFileStripe fileStripe);

    int[] Indexes { get; }
}