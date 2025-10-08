using System.ComponentModel.DataAnnotations;

namespace LanCloud.Database.Entities;

public class User
{
    [Key]
    public long Id { get; set; }

    public string UserName { get; set; } = string.Empty;
}
