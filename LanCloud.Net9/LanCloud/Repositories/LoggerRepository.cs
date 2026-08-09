using LanCloud.Domain;
using LanCloud.Interfaces;

namespace LanCloud.Repositories;

public class LoggerRepository
{
    public LoggerRepository(string currentDirectory)
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
