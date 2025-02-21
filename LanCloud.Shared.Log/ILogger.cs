using System;

namespace LanCloud.Shared.Log
{
    public interface ILogger : IDisposable
    {
        bool LogInfo { get; set; }

        string Error(string message);
        string Error(Exception ex);
        string Info(string message);
    }
}