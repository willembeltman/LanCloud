
namespace LanCloud.Interfaces;

public interface IFileStripe
{
    string Extention { get; }
    int[] Indexes { get; }
    bool IsTemp { get; }
    string? Hash { get; }
    long? Length { get; }
    string Name { get; }

    Stream OpenRead();
    Stream OpenWrite();
    void Update(long length, string hash);
}