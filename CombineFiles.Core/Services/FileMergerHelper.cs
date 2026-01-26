using System;
using System.IO;
using System.Security.Cryptography;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.Core.Services;

/// <summary>
/// Helper condiviso per eliminare duplicazione di codice tra FileMerger e PaginatedFileMerger.
/// </summary>
internal static class FileMergerHelper
{
    /// <summary>
    /// Risultato della validazione iniziale di un file da unire.
    /// </summary>
    public enum ValidationResult
    {
        /// <summary>Il file può essere processato normalmente.</summary>
        Process,
        /// <summary>Il file è un duplicato e deve essere saltato.</summary>
        SkipDuplicate,
        /// <summary>Il file deve essere saltato per altre ragioni (es. budget violato).</summary>
        Skip,
        /// <summary>Il file deve solo mostrare l'header (listOnlyFileNames).</summary>
        HeaderOnly
    }

    /// <summary>
    /// Valida e prepara un file per il merge, gestendo validazione, deduplicazione e listOnlyFileNames.
    /// </summary>
    /// <param name="filePath">Percorso del file da validare.</param>
    /// <param name="baseDir">Directory base per calcolare il percorso relativo.</param>
    /// <param name="avoidDuplicatesByHash">Se true, controlla i duplicati.</param>
    /// <param name="listOnlyFileNames">Se true, mostra solo l'header.</param>
    /// <param name="isDuplicate">Funzione per verificare se il file è un duplicato.</param>
    /// <param name="shouldSkip">Funzione per verificare se il file deve essere saltato (es. budget violato).</param>
    /// <param name="writeHeader">Funzione per scrivere l'header del file.</param>
    /// <param name="relativePath">Output: percorso relativo del file.</param>
    /// <returns>Risultato della validazione che indica come procedere.</returns>
    public static ValidationResult ValidateAndPrepareFile(
        string filePath,
        string baseDir,
        bool avoidDuplicatesByHash,
        bool listOnlyFileNames,
        Func<string, bool> isDuplicate,
        Func<bool> shouldSkip,
        Action<string> writeHeader,
        out string relativePath)
    {
        relativePath = string.Empty;

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException(nameof(filePath));

        // Controlla se deve essere saltato (es. budget violato)
        if (shouldSkip())
            return ValidationResult.Skip;

        // Deduplicazione opzionale
        if (avoidDuplicatesByHash && isDuplicate(filePath))
            return ValidationResult.SkipDuplicate;

        relativePath = FileHelper.GetRelativePath(baseDir, filePath);

        // Se listOnlyFileNames, mostra solo l'header
        if (listOnlyFileNames)
        {
            writeHeader(relativePath);
            return ValidationResult.HeaderOnly;
        }

        return ValidationResult.Process;
    }

    /// <summary>
    /// Verifica se un file è un duplicato basandosi sul suo hash SHA256.
    /// </summary>
    /// <param name="filePath">Percorso del file da verificare.</param>
    /// <param name="sha">Istanza SHA256 per calcolare l'hash.</param>
    /// <param name="processedHashes">Set degli hash già processati.</param>
    /// <returns>True se il file è un duplicato, false altrimenti.</returns>
    public static bool IsDuplicate(string filePath, SHA256 sha, System.Collections.Generic.HashSet<string> processedHashes)
    {
        try
        {
            using var fs = File.OpenRead(filePath);
            var hash = Convert.ToBase64String(sha.ComputeHash(fs));
            return !processedHashes.Add(hash);
        }
        catch
        {
            return false;
        }
    }
}
