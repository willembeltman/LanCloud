using LanCloud.Database.Entities;
using LanCloud.Domain.Application;
using LanCloud.Interfaces;

namespace LanCloud.Services;

public class AuthenticationService(LocalApplication application, ILogger logger)
{
    public ILogger Logger { get; } = logger;

    public User ValidateUser(string userName, string password)
    {
        //if (userName != "willem") return null;
        return new User()
        {
            Id = 1,
            UserName = userName,
        };
    }
}