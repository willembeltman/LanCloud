namespace LanCloud.Models.Config;

public class LocalShareBitConfig
{
    public LocalShareBitConfig() { }
    public LocalShareBitConfig(params int[] indexes)
    {
        Indexes = indexes;
    }

    public int[] Indexes { get; set; } = [];
}