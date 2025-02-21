namespace LanCloud.Domain.FileRef
{
    public class FileRefMetadata
    {
        public FileRefMetadata(long? length, string hash, FileRefStripeMetadata[] stripes)
        {
            Length = length;
            Hash = hash;
            Stripes = stripes;
        }

        public FileRefMetadata(LocalFileRef pathInfo)
        {
            Length = pathInfo.Metadata?.Length;
            Hash = pathInfo.Metadata?.Hash;
            Stripes = pathInfo.Metadata?.Stripes;
        }

        public long? Length { get; }
        public string Hash { get; }
        public FileRefStripeMetadata[] Stripes { get; }
    }
}