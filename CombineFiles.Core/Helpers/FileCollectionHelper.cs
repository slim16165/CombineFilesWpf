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
                filesToProcess = collector.CollectFilesByExtensions(
                    sourcePath,
                    options.Recurse,
                    options.Extensions,
                    options.MaxTotalTokens > 0 ? options.MaxTotalTokens : null);
                logger.WriteLog($"File trovati dopo filtro estensioni: {filesToProcess.Count}", LogLevel.INFO);
                break;

            case "regex":
                filesToProcess = collector.CollectFilesByRegex(
                    sourcePath,
                    options.Recurse,
                    options.RegexPatterns,
                    options.MaxTotalTokens > 0 ? options.MaxTotalTokens : null);
                logger.WriteLog($"File trovati dopo filtro regex: {filesToProcess.Count}", LogLevel.INFO);
                break;

            case "interactiveselection":
                filesToProcess = HandleInteractiveMode(options, logger, collector, sourcePath);
                break;

            default:
                // Se non specificato, raccogli tutti i file
                filesToProcess = GetAllFiles(collector, sourcePath, options);
                break;
        }

        // Applichiamo sempre il filtro per dimensione (MinSize/MaxSize) e data
        var filterService = new FileFilterService(logger);
        filesToProcess = filterService.FilterFiles(filesToProcess, options);
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

}