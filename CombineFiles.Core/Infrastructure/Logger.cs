using System;
using System.Diagnostics;
using System.IO;
using Spectre.Console;

namespace CombineFiles.Core.Infrastructure
{
    public class Logger
    {
        private readonly string _logFile;
        private readonly bool _enabled;

        // Aggiunta: livello minimo di log
        public LogLevel MinimumLogLevel { get; }


        public Logger(string logFile, bool enabled, LogLevel minimumLogLevel = LogLevel.INFO)
        {
            _logFile = logFile;
            _enabled = enabled;
            MinimumLogLevel = minimumLogLevel;
        }

        /// <summary>
        /// Logga un messaggio su file (se abilitato e livello >= MinimumLogLevel) e lo mostra a console con colori diversi
        /// in base al livello. Il livello è passato come stringa e convertito in enum.
        /// </summary>
        public void WriteLog(string message, string level)
        {
            if (!Enum.TryParse(level, true, out LogLevel logLevel))
                logLevel = LogLevel.INFO;

            // Se il livello del messaggio è minore di quello impostato, non loggare
            if (logLevel > MinimumLogLevel)
                return;

            LogInternal(message, logLevel);
        }

        /// <summary>
        /// Overload che accetta direttamente il LogLevel e sceglie il colore tramite Lev2Color.
        /// </summary>
        public void WriteLog(string message, LogLevel logLevel)
        {
            if (logLevel > MinimumLogLevel)
                return;

            LogInternal(message, logLevel);
        }

        /// <summary>
        /// Metodo interno per gestire il log su file e console.
        /// </summary>
        private void LogInternal(string message, LogLevel logLevel)
        {
            // Log su file
            if (_enabled)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string logMessage = $"{timestamp} [{logLevel}] {message}";
                    File.AppendAllText(_logFile, logMessage + Environment.NewLine);
                }
                catch
                {
                    // Se fallisce la scrittura su file, gestire l’errore (qui viene ignorato)
                }
            }

            // Visualizzazione a console con colore scelto tramite Lev2Color
            string color = Lev2Color(logLevel);
            AnsiConsole.MarkupLine($"[{color}]{message}[/]");
        }

        /// <summary>
        /// Restituisce il colore associato al LogLevel.
        /// </summary>
        private string Lev2Color(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.INFO => "blue",
                LogLevel.WARNING => "yellow",
                LogLevel.ERROR => "red",
                LogLevel.DEBUG => "grey",
                _ => "white",
            };
        }
    }
}
