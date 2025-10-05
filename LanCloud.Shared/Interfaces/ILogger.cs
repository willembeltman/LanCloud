using System;

namespace LanCloud.Shared.Interfaces
{
    public interface ILogger : DokanNet.Logging.ILogger, IDisposable
    {
        bool LogInfo { get; set; }

        string Error(string message);
        string Error(Exception ex);
        string Info(string message);
    }
}