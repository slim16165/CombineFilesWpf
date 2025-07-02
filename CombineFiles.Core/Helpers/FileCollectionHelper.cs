using CombineFiles.Core.Configuration;
using CombineFiles.Core.Infrastructure;
using CombineFiles.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace CombineFiles.Core.Helpers;

public static class FileCollectionHelper
{
    /// <summary>
    /// Metodo "unico" che, in base a options.Mode, decide come ottenere la lista di file
    /// e applica filtraggio per dimensione (MinSize/MaxSize).
    /// </summary>
    public static List<string> CollectFiles(CombineFilesOptions options, Logger logger, FileCollector collector, string sourcePath)
    {
        List<string> filesToProcess;

        switch (options.Mode?.ToLowerInvariant())
        {
            case "list":
                filesToProcess = HandleListMode(options, logger);
                break;

            case "extensions":
                filesToProcess = HandleFilterMode(options, logger, collector,
                    file => options.Extensions.Any(ext => file.Path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)),
                    "estensioni");
                break;

            case "regex":
                filesToProcess = HandleFilterMode(options, logger, collector,
                    file => options.RegexPatterns.Any(pattern => Regex.IsMatch(file.Path, pattern)),
                    "regex");
                break;

            case "interactiveselection":
                filesToProcess = HandleInteractiveMode(options, logger, collector, sourcePath);
                break;

            default:
                // Se non specificato, raccogli tutti i file
                filesToProcess = GetAllFiles(collector, sourcePath, options);
                break;
        }

        // Applichiamo sempre il filtro per dimensione (MinSize/MaxSize)
        filesToProcess = FilterBySize(filesToProcess, options, logger);
        return filesToProcess;
    }

    private static List<string> HandleListMode(CombineFilesOptions options, Logger logger)
    {
        var filesToProcess = new List<string>();
        string basePath = Directory.GetCurrentDirectory();

        foreach (var relativeFile in options.FileList)
        {
            string absPath = Path.IsPathRooted(relativeFile)
                ? relativeFile
                : Path.Combine(basePath, relativeFile);

            if (File.Exists(absPath))
            {
                logger.WriteLog($"File incluso dalla lista: {absPath}", LogLevel.INFO);
                filesToProcess.Add(absPath);
            }
            else
            {
                logger.WriteLog($"File non trovato: {absPath}", LogLevel.WARNING);
                Console.WriteLine($"Avviso: File non trovato: {absPath}");
            }
        }

        return filesToProcess;
    }

    /// <summary>
    /// Gestisce modalità con filtri (extensions e regex) eliminando la duplicazione di codice.
    /// Il conteggio dei token viene fatto DOPO il filtraggio.
    /// </summary>
    private static List<string> HandleFilterMode(CombineFilesOptions options, Logger logger, FileCollector collector,
        Func<CollectedFileInfo, bool> filterPredicate, string filterType)
    {
        var basePath = Directory.GetCurrentDirectory();

        // 1. Raccogli tutti i file (senza limite token)
        var allFiles = collector.GetAllFilesWithTokenInfo(basePath, options.Recurse, null);

        // 2. Filtra per estensione/regex
        var filtered = allFiles.IncludedFiles
            .Where(filterPredicate)
            .ToList();

        //while (!Debugger.IsAttached) Thread.Sleep(100);
        //Debugger.Break();

        logger.WriteLog($"File trovati dopo filtro {filterType}: {filtered.Count}", LogLevel.INFO);

        // 3. Applica limite token SUI FILE FILTRATI
        if (options.MaxTotalTokens > 0)
        {
            // Applica il limite di token sui file già filtrati
            var result = collector.ApplyTokenLimitToFilteredFiles(filtered, options.MaxTotalTokens, FilePriorityStrategy.SizeAscending);
            logger.WriteLog($"Nota: la modalità partial/exclude (PartialFileMode) viene applicata solo durante il merge, non in questa fase di raccolta file.", LogLevel.DEBUG);
            return result;
        }

        return filtered.Select(f => f.Path).ToList();
    }

    /// <summary>
    /// Gestisce la modalità interattiva raccogliendo tutti i file disponibili
    /// </summary>
    private static List<string> HandleInteractiveMode(CombineFilesOptions options, Logger logger, FileCollector collector, string sourcePath)
    {
        var allFiles = GetAllFiles(collector, sourcePath, options);
        return collector.StartInteractiveSelection(allFiles, sourcePath);
    }

    /// <summary>
    /// Metodo helper unificato per raccogliere tutti i file con gestione opzionale dei token
    /// </summary>
    private static List<string> GetAllFiles(FileCollector collector, string sourcePath, CombineFilesOptions options)
    {
        return options.MaxTotalTokens > 0
            ? collector.GetAllFiles(sourcePath, options.Recurse, options.MaxTotalTokens)
            : collector.GetAllFiles(sourcePath, options.Recurse);
    }

    /// <summary>
    /// Filtra la lista di file in base a MinSize/MaxSize (convertiti in byte).
    /// Logga a livello INFO quanti file sono stati rimossi per dimensione.
    /// </summary>
    private static List<string> FilterBySize(List<string> files, CombineFilesOptions options, Logger logger)
    {
        var (minBytes, maxBytes) = ParseSizeLimits(options, logger);

        if (maxBytes <= 0 && minBytes <= 0)
            return files;

        var filtered = new List<string>();
        int removedCount = 0;

        foreach (var path in files)
        {
            if (!TryGetFileSize(path, out long length, logger))
                continue;

            if (IsFileSizeExcluded(length, minBytes, maxBytes, path, logger))
            {
                removedCount++;
                continue;
            }

            filtered.Add(path);
        }

        logger.WriteLog($"Filtraggio dimensione: rimossi {removedCount} file, rimangono {filtered.Count}", LogLevel.INFO);
        return filtered;
    }

    /// <summary>
    /// Estrae e valida i limiti di dimensione dalle opzioni
    /// </summary>
    private static (long minBytes, long maxBytes) ParseSizeLimits(CombineFilesOptions options, Logger logger)
    {
        long maxBytes = 0;
        long minBytes = 0;

        if (!string.IsNullOrWhiteSpace(options.MaxSize))
        {
            try
            {
                maxBytes = FileHelper.ConvertSizeToBytes(options.MaxSize);
            }
            catch (Exception ex)
            {
                logger.WriteLog($"Formato MaxSize non valido ('{options.MaxSize}'): {ex.Message}", LogLevel.WARNING);
            }
        }

        if (!string.IsNullOrWhiteSpace(options.MinSize))
        {
            try
            {
                minBytes = FileHelper.ConvertSizeToBytes(options.MinSize);
            }
            catch (Exception ex)
            {
                logger.WriteLog($"Formato MinSize non valido ('{options.MinSize}'): {ex.Message}", LogLevel.WARNING);
            }
        }

        return (minBytes, maxBytes);
    }

    /// <summary>
    /// Tenta di ottenere la dimensione del file gestendo eventuali errori
    /// </summary>
    private static bool TryGetFileSize(string path, out long length, Logger logger)
    {
        length = 0;
        try
        {
            length = new System.IO.FileInfo(path).Length;
            return true;
        }
        catch (Exception ex)
        {
            logger.WriteLog($"Impossibile leggere dimensione file '{path}': {ex.Message}", LogLevel.WARNING);
            return false;
        }
    }

    /// <summary>
    /// Verifica se un file deve essere escluso in base alla sua dimensione
    /// </summary>
    private static bool IsFileSizeExcluded(long fileSize, long minBytes, long maxBytes, string path, Logger logger)
    {
        if (maxBytes > 0 && fileSize > maxBytes)
        {
            logger.WriteLog($"Escludo per dimensione > MaxSize: {path} ({fileSize} byte)", LogLevel.DEBUG);
            return true;
        }

        if (minBytes > 0 && fileSize < minBytes)
        {
            logger.WriteLog($"Escludo per dimensione < MinSize: {path} ({fileSize} byte)", LogLevel.DEBUG);
            return true;
        }

        return false;
    }
}