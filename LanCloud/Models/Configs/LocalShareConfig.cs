namespace LanCloud.Models.Configs
{
    public class LocalShareConfig
    {
        public string DirectoryName { get; set; }
        public int MaxSpeed { get; set; }
        public LocalShareBitConfig[] Parts { get; set; }
        public bool IsSSD { get; internal set; }
    }
}