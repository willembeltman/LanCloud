namespace LanCloud.Models.Config;

public class LocalShareConfig
{
    public string DirectoryName { get; set; } = string.Empty;
    public int MaxSpeed { get; set; }
    public LocalShareBitConfig[] Parts { get; set; } = [];
    public bool IsSSD { get; set; }
}