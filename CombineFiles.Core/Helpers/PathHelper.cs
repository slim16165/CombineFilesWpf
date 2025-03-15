using System.Collections.Generic;
using System.IO;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.Core.Helpers;

public static class PathHelper
{
    /// <summary>
    /// Normalizza i percorsi da escludere in percorsi assoluti, loggando eventuali avvisi se non trovati.
    /// </summary>
    public static List<string> NormalizeExcludePaths(List<string> excludePaths, string basePath, Logger logger)
    {
        var fullExcludePaths = new List<string>();
        if (excludePaths == null) return fullExcludePaths;

        foreach (var p in excludePaths)
        {
            string candidate = p;
            if (!Path.IsPathRooted(candidate))
                candidate = Path.Combine(basePath, p);

            if (Directory.Exists(candidate))
            {
                fullExcludePaths.Add(candidate);
                logger.WriteLog($"Percorso escluso aggiunto: {candidate}", LogLevel.DEBUG);
            }
            else
            {
                // Se non esiste come directory, avvisa
                logger.WriteLog($"Directory di esclusione non trovata o non valida: {candidate}", LogLevel.WARNING);
            }
        }
        return fullExcludePaths;
    }
}