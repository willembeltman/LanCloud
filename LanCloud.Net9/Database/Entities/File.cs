using System.ComponentModel.DataAnnotations;
namespace LanCloud.Database.Entities;

public class File
{
    [Key]
    public long Id { get; set; }

    public long FolderId { get; set; }
}
