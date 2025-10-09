namespace LanCloud.Interfaces;

public interface IShare
{
    IFileStripe? FindFileStripe(string extention, string hash, long length, int[] indexes);

    int[] Indexes { get; }
}