using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CombineFiles.Core.Configuration;
using CombineFiles.Core.Infrastructure;
using CombineFiles.Core.Services;

namespace CombineFiles.Core.Helpers;

public static class FileCollectionHelper
{
    /// <summary>
    /// Metodo “unico” che, in base a options.Mode, decide come ottenere la lista di file
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
                filesToProcess = HandleExtensionsMode(options, logger, collector);
                break;

            case "regex":
                filesToProcess = HandleRegexMode(options, logger, collector);
                break;

            case "interactiveselection":
                // Ottieni prima i file per estensione
                filesToProcess = HandleExtensionsMode(options, logger, collector);
                // Poi avvia la selezione interattiva
                filesToProcess = collector.StartInteractiveSelection(filesToProcess, sourcePath);
                break;

            default:
                // Se non specificato, raccogli tutti i file
                filesToProcess = collector.GetAllFiles(sourcePath, options.Recurse);
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

    private static List<string> HandleExtensionsMode(CombineFilesOptions options, Logger logger, FileCollector collector)
    {
        var basePath = Directory.GetCurrentDirectory();
        var allFiles = collector.GetAllFiles(basePath, options.Recurse);
        var matched = new List<string>();

        foreach (var file in allFiles)
        {
            foreach (var ext in options.Extensions)
            {
                if (file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    matched.Add(file);
                    break;
                }
            }
        }

        matched = matched.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        logger.WriteLog($"File da processare dopo filtraggio per estensioni: {matched.Count}", LogLevel.INFO);
        return matched;
    }

    private static List<string> HandleRegexMode(CombineFilesOptions options, Logger logger, FileCollector collector)
    {
        var basePath = Directory.GetCurrentDirectory();
        var allFiles = collector.GetAllFiles(basePath, options.Recurse);
        var matched = new List<string>();

        foreach (var file in allFiles)
        {
            foreach (var pattern in options.RegexPatterns)
            {
                if (Regex.IsMatch(file, pattern))
                {
                    matched.Add(file);
                    break; // Evita duplicati
                }
            }
        }

        matched = matched.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        logger.WriteLog($"File da processare (regex): {matched.Count}", LogLevel.INFO);
        return matched;
    }

    /// <summary>
    /// Filtra la lista di file in base a MinSize/MaxSize (convertiti in byte).
    /// Logga a livello INFO quanti file sono stati rimossi per dimensione.
    /// </summary>
    private static List<string> FilterBySize(List<string> files, CombineFilesOptions options, Logger logger)
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

        if (maxBytes <= 0 && minBytes <= 0)
            return files;

        var filtered = new List<string>();
        int removedCount = 0;

        foreach (var path in files)
        {
            long length;
            try
            {
                length = new FileInfo(path).Length;
            }
            catch (Exception ex)
            {
                logger.WriteLog($"Impossibile leggere dimensione file '{path}': {ex.Message}", LogLevel.WARNING);
                continue;
            }

            if (maxBytes > 0 && length > maxBytes)
            {
                removedCount++;
                logger.WriteLog($"Escludo per dimensione > MaxSize: {path} ({length} byte)", LogLevel.DEBUG);
                continue;
            }

            if (minBytes > 0 && length < minBytes)
            {
                removedCount++;
                logger.WriteLog($"Escludo per dimensione < MinSize: {path} ({length} byte)", LogLevel.DEBUG);
                continue;
            }

            filtered.Add(path);
        }

        logger.WriteLog($"Filtraggio dimensione: rimossi {removedCount} file, rimangono {filtered.Count}", LogLevel.INFO);
        return filtered;
    }
}
