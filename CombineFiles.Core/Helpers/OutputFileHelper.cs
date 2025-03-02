using System.IO;
using System.Text;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.ConsoleApp.Helpers
{
    public static class OutputFileHelper
    {
        /// <summary>
        /// Svuota o crea il file di output (equivalente a "Out-File -Force" in PowerShell).
        /// </summary>
        public static void PrepareOutputFile(string outputFile, string outputFormat, Encoding encoding, Logger logger)
        {
            // Per semplicità, trattiamo .txt, .csv e .json allo stesso modo. 
            // Se vuoi un header CSV o un array JSON iniziale, gestiscilo qui.
            File.WriteAllText(outputFile, string.Empty, encoding);
            logger.WriteLog($"File di output creato/svuotato: {outputFile}", "INFO");

            // Esempio di aggiunta header CSV:
            // if (outputFormat.Equals("csv", StringComparison.OrdinalIgnoreCase))
            // {
            //     File.AppendAllText(outputFile, "col1,col2\r\n", encoding);
            // }
        }
    }
}