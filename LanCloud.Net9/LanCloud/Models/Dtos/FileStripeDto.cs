using LanCloud.Interfaces;

namespace LanCloud.Models.Dtos;

public class FileStripeDto
{
    public FileStripeDto() { }
    public FileStripeDto(IFileStripe fileStripe)
    {
        if (fileStripe.Length == null) throw new ArgumentNullException(nameof(fileStripe));
        Name = fileStripe.Name;
        Extention = fileStripe.Extention;
        Hash = fileStripe.Hash;
        Length = fileStripe.Length.Value;
        Indexes = fileStripe.Indexes;
        IsTemp = fileStripe.IsTemp;
    }

    public string Name { get; set; } = string.Empty;
    public string Extention { get; set; } = string.Empty;
    public int[] Indexes { get; set; } = [];
    public bool IsTemp { get; set; }
    public long? Length { get; set; }
    public string? Hash { get; set; }
}