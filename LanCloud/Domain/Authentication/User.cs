using LanCloud.Models;

namespace LanCloud.Domain.Authentication
{
    public class User : IFtpUser
    {
        public User(string userName)
        {
            UserName = userName;
        }

        public string UserName { get; set; }
    }
}
