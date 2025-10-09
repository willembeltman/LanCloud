namespace LanCloud.Interfaces;

public interface IShare
{
    IShareStripe[] ShareStripes { get; }

    IFileStripe? FindFileStripe(string extention, string hash, long length, int[] indexes);
}