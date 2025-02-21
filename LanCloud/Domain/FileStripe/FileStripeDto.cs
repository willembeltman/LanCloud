namespace LanCloud.Domain.FileStripe
{
    public class FileStripeDto
    {
        public FileStripeDto() { }
        public FileStripeDto(IFileStripe fileStripe)
        {
            Extention = fileStripe.Extention;
            Hash = fileStripe.Hash;
            Length = fileStripe.Length;
            Indexes = fileStripe.Indexes;
            IsTemp = fileStripe.IsTemp;
        }

        public string Extention { get; set; }
        public string Hash { get; set; }
        public long Length { get; set; }
        public int[] Indexes { get; set; }
        public bool IsTemp { get; set; }
    }
}