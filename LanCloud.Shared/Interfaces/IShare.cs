using LanCloud.Domain.FileStripe;

namespace LanCloud.Domain.Share
{
    public interface IShare
    {
        IShareStripe[] ShareStripes { get; }

        IFileStripe FindFileStripe(string extention, string hash, long length, int[] indexes);
    }
}