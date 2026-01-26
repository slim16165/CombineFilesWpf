using System;
using System.IO;

namespace CombineFiles.Core.Services;

/// <summary>
/// Servizio centralizzato per la stima dei token nei file.
/// Elimina la duplicazione di codice tra FileCollector e AnalyticsService.
/// </summary>
public static class TokenEstimationService
{
    /// <summary>
    /// Stima il numero di token per un file basandosi sulla sua estensione e dimensione.
    /// </summary>
    /// <param name="filePath">Percorso del file (usato per logging errori se necessario).</param>
    /// <param name="fileInfo">Informazioni sul file.</param>
    /// <returns>Numero stimato di token (minimo 1).</returns>
    public static int EstimateTokens(string filePath, FileInfo fileInfo)
    {
        if (fileInfo == null)
            return 1;

        try
        {
            // Usa una stima più accurata basata sul tipo di file
            string extension = fileInfo.Extension.ToLower();
            double tokensPerByte = GetTokensPerByte(extension);

            // Per file molto piccoli, usa un minimo di token
            int estimatedTokens = (int)Math.Ceiling(fileInfo.Length * tokensPerByte);
            return Math.Max(estimatedTokens, 1);
        }
        catch
        {
            // In caso di errore, ritorna almeno 1 token
            return 1;
        }
    }

    /// <summary>
    /// Ottiene il rapporto token/byte in base all'estensione del file.
    /// </summary>
    /// <param name="extension">Estensione del file (es. ".cs", ".txt").</param>
    /// <returns>Rapporto token per byte.</returns>
    private static double GetTokensPerByte(string extension)
    {
        return extension switch
        {
            ".cs" or ".java" or ".cpp" or ".c" or ".h" => 0.20,     // Codice: più denso
            ".py" or ".js" or ".ts" => 0.22,                        // Script: abbastanza denso
            ".xml" or ".html" or ".xaml" => 0.15,                   // Markup: molto verboso
            ".json" or ".yaml" or ".yml" => 0.18,                   // Dati strutturati
            ".txt" or ".md" or ".rst" => 0.25,                      // Testo normale
            ".sql" => 0.20,                                          // SQL
            ".css" or ".scss" or ".less" => 0.18,                   // Stili
            ".log" => 0.25,                                          // Log files
            ".config" or ".ini" or ".properties" => 0.20,           // Configuration
            _ => 0.25                                                // Default: assume testo
        };
    }
}
