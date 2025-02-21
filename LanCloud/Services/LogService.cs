using LanCloud.Shared.Log;
using System.IO;

namespace LanCloud.Services
{
    public class LogService
    {
        public LogService(string currentDirectory)
        {
            CurrentDirectory = currentDirectory;
        }

        public string CurrentDirectory { get; }

        public ILogger Create()
        {
            var fullname = Path.Combine(CurrentDirectory, "log.txt");
            return new Logger(fullname);
        }
    }
}
