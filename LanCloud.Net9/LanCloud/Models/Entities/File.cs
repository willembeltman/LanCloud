using System.ComponentModel.DataAnnotations;
namespace LanCloud.Models.Entities;

public class File
{
    [Key]
    public long Id { get; set; }

    public long FolderId { get; set; }
}
