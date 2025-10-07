using System.IO;

namespace LanCloud.Domain.FileStripe
{
    public interface IFileStripe
    {
        string Extention { get; }
        string Hash { get; }
        long Length { get; }
        int[] Indexes { get; }
        bool IsTemp { get; }

        FileStream OpenRead();
    }
}