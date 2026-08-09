namespace LanCloud.Domain.IO;

public class FileStripeMetadata
{
    public FileStripeMetadata(int[] indexes)
    {
        Indexes = indexes;
    }

    public int[] Indexes { get; }

    public string GetUniqueIdentifier()
    {
        return string.Join("_", Indexes.OrderBy(a => a));
    }
}
