using LanCloud.Models.Entities;

namespace LanCloud.Domain.Application;

public class Authentication(LocalApplication application)
{
    public LocalApplication Application { get; } = application;
    public User ValidateUser(string? userName, string? password)
    {
        //if (userName != "willem") return null;
        return new User()
        {
            Id = 1,
            UserName = "Admin",
        };
    }
}