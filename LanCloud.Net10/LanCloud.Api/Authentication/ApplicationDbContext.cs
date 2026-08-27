using gAPI.Core.Server.Entities;
using Microsoft.EntityFrameworkCore;

namespace LanCloud.Api.Authentication;

public class ApplicationDbContext : AuthenticationDbContext<User>
{
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }
}
