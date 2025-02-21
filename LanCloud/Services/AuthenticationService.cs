using LanCloud.Domain.Application;
using LanCloud.Domain.Authentication;
using LanCloud.Models;
using LanCloud.Shared.Log;

namespace LanCloud
{
    public class AuthenticationService
    {
        public AuthenticationService(LocalApplication localApplication, ILogger logger)
        {
            Logger = logger;
        }

        public ILogger Logger { get; }

        public IFtpUser ValidateUser(string userName, string password)
        {
            //if (userName != "willem") return null;
            return new User(userName);
        }
    }
}