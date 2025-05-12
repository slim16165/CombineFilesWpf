using System.IO;
using System.Text;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.Core.Helpers;

public static class OutputFileHelper
{
    /// <summary>
    /// Svuota o crea il file di output (equivalente a "Out-File -Force" in PowerShell).
    /// </summary>
    public static void PrepareOutputFile(string? outputFile, Encoding encoding, Logger logger)
    {
        File.WriteAllText(outputFile, string.Empty, encoding);
        logger.WriteLog($"File di output creato/svuotato: {outputFile}", LogLevel.INFO);
    }
}