using System.ComponentModel.DataAnnotations;

namespace LanCloud.Database.Entities;

public class Folder
{
    [Key]
    public long Id {  get; set; }
}
