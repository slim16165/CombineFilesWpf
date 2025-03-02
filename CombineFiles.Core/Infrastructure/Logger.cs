using System;
using System.IO;

namespace CombineFiles.Core.Infrastructure;

public class Logger
{
    private readonly string _logFile;
    private readonly bool _enabled;

    public Logger(string logFile, bool enabled)
    {
        _logFile = logFile;
        _enabled = enabled;
    }

    public void WriteLog(string message, string level)
    {
        if (!_enabled)
            return;

        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logMessage = $"{timestamp} [{level}] {message}";
            File.AppendAllText(_logFile, logMessage + Environment.NewLine);
        }
        catch
        {
            // In un'app reale, qui potresti gestire l'errore di scrittura del log.
        }
    }
}