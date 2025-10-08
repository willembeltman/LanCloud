using gAPI.EntityFrameworkDisk;

namespace LanCloud.Database;

#nullable disable

public class ApplicationDbContext : DbContext
{
    public DbSet<Entities.User> Users { get; set; }
    public DbSet<Entities.File> Files { get; set; }
    public DbSet<Entities.Folder> Folders { get; set; }
}
