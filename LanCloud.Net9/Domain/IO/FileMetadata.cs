using LanCloud.Domain.Local;

namespace LanCloud.Domain.IO;

public class FileMetadata
{
    public FileMetadata(int bufferSize, long length, string hash, FileStripeMetadata[] stripes)
    {
        BufferSize = bufferSize;
        Length = length;
        Hash = hash;
        Stripes = stripes;
    }

    public FileMetadata(LocalFile pathInfo)
    {
        if (pathInfo.Metadata == null) throw new ArgumentNullException("Metadata cannot be null");
        BufferSize = pathInfo.Metadata.BufferSize;
        Length = pathInfo.Metadata.Length;
        Hash = pathInfo.Metadata.Hash;
        Stripes = pathInfo.Metadata.Stripes;
    }

    public int BufferSize { get; }
    public long Length { get; }
    public string Hash { get; }
    public FileStripeMetadata[] Stripes { get; }
}