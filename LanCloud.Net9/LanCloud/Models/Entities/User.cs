using System.ComponentModel.DataAnnotations;

namespace LanCloud.Models.Entities;

public class User
{
    [Key]
    public long Id { get; set; }

    public string UserName { get; set; } = string.Empty;
}
