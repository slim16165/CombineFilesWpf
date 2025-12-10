using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CombineFiles.ConsoleApp.Helpers;
using CombineFiles.Core;
using CombineFiles.Core.Configuration;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Infrastructure;
using CombineFiles.Core.Services;
using Spectre.Console;

namespace CombineFiles.ConsoleApp.Extensions;

/// <summary>
/// Contiene la logica di esecuzione principale (Execute) e i metodi di supporto.
/// </summary>
public static class ExecutionFlow
{
    /// <summary>
    /// Entry point del programma, esegue la logica di combinazione dei file.
    /// Viene chiamato dal SetHandler del RootCommand.
    /// </summary>
    public static void Execute(CombineFilesOptions options)
    {
        if (HandleHelpAndPresets(options)) return;
        if (!ApplyPresetSafely(options)) return;

        var logger = InitializeLogger(options);

        if (!ValidateOptions(options, logger)) return;

        string sourcePath = Directory.GetCurrentDirectory();
        var normalizedExcludePaths = PathHelper.NormalizeExcludePaths(options.ExcludePaths, sourcePath, logger);

        ExcludeOutputFileIfNeeded(options, logger);

        var filesToProcess = CollectFiles(options, logger, normalizedExcludePaths, sourcePath);
        filesToProcess = FilterFiles(filesToProcess, options, logger);

        if (filesToProcess.Count == 0)
        {
            logger.WriteLog("Nessun file trovato per l'unione.", LogLevel.WARNING);
            Console.WriteLine("Nessun file trovato per l'unione.");
            return;
        }

        if (!PrepareOutputFile(options, logger)) return;

        // Genera report analytics se richiesto
        if (options.Debug || options.EnableLog)
        {
            var analytics = new AnalyticsService();
            var stats = analytics.AnalyzeFiles(filesToProcess);
            logger.WriteLog(analytics.GenerateTextReport(stats), LogLevel.INFO);
            
            // Salva anche report HTML se non è output a console
            if (!options.OutputToConsole && !string.IsNullOrWhiteSpace(options.OutputFile))
            {
                string htmlReportPath = Path.ChangeExtension(options.OutputFile, ".html");
                try
                {
                    File.WriteAllText(htmlReportPath, analytics.GenerateHtmlReport(stats), Encoding.UTF8);
                    logger.WriteLog($"Report HTML generato: {htmlReportPath}", LogLevel.INFO);
                }
                catch (Exception ex)
                {
                    logger.WriteLog($"Errore nella generazione del report HTML: {ex.Message}", LogLevel.WARNING);
                }
            }
        }

        MergeFiles(filesToProcess, options, logger);
    }

    #region Metodi Helper di "Execute"

    private static bool HandleHelpAndPresets(CombineFilesOptions options)
    {
        if (options.Help)
        {
            ParameterHelper.PrintHelp();
            return true;
        }
        if (options.ListPresets)
        {
            ParameterHelper.PrintPresetList();
            return true;
        }
        return false;
    }

    private static bool ApplyPresetSafely(CombineFilesOptions options)
    {
        try
        {
            PresetManager.ApplyPreset(options);
            return true;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteColored(ex.Message, ConsoleColor.Red);
            return false;
        }
    }

    private static Logger InitializeLogger(CombineFilesOptions options)
    {
        string sourcePath = Directory.GetCurrentDirectory();
        string logFilePath = Path.Combine(sourcePath, "CombineFiles.log");
        var logger = new Logger(
            logFile: logFilePath,
            enabled: options.EnableLog,
            minimumLogLevel: LogLevel.INFO
        );
        return logger;
    }

    private static bool ValidateOptions(CombineFilesOptions options, Logger logger)
    {
        return ParameterHelper.ValidateParameters(options, logger);
    }

    private static void ExcludeOutputFileIfNeeded(CombineFilesOptions options, Logger logger)
    {
        if (!options.OutputToConsole && !string.IsNullOrWhiteSpace(options.OutputFile))
        {
            var outFileName = Path.GetFileName(options.OutputFile);
            if (!string.IsNullOrEmpty(outFileName))
            {
                options.ExcludeFiles.Add(outFileName);
                logger.WriteLog($"Aggiunto {outFileName} alla lista dei file esclusi (per evitare conflitti).", LogLevel.DEBUG);
            }
        }
    }

    private static List<string> CollectFiles(
        CombineFilesOptions options,
        Logger logger,
        List<string> normalizedExcludePaths,
        string sourcePath)
    {
        logger.WriteLog("Inizio raccolta dei file da processare", LogLevel.INFO);
        
        // Normalizza anche IncludePaths se presenti
        var normalizedIncludePaths = options.IncludePaths != null && options.IncludePaths.Count > 0
            ? PathHelper.NormalizeExcludePaths(options.IncludePaths, sourcePath, logger)  // Usa stesso helper
            : new List<string>();
        
        var fileCollector = new FileCollector(
            logger,
            normalizedIncludePaths,
            normalizedExcludePaths,
            options.ExcludeFiles,
            options.ExcludeFilePatterns
        );
        var filesToProcess = FileCollectionHelper.CollectFiles(options, logger, fileCollector, sourcePath);
        logger.WriteLog($"Trovati {filesToProcess.Count} file da processare.", LogLevel.INFO);
        return filesToProcess;
    }

    private static List<string> FilterFiles(
        List<string> files,
        CombineFilesOptions options,
        Logger logger)
    {
        var filterService = new FileFilterService(logger);
        
        // Filtra per dimensione e data
        var filtered = filterService.FilterFiles(files, options);

        // Escludiamo i file con tag <auto-generated>
        filtered = filterService.FilterAutoGenerated(filtered);

        return filtered;
    }

    private static bool PrepareOutputFile(CombineFilesOptions options, Logger logger)
    {
        if (options.OutputToConsole || string.IsNullOrWhiteSpace(options.OutputFile))
            return true;

        try
        {
            Encoding selectedEncoding = Encoding.UTF8;
            OutputFileHelper.PrepareOutputFile(options.OutputFile, selectedEncoding, logger);
            return true;
        }
        catch (Exception ex)
        {
            logger.WriteLog($"Impossibile creare/scrivere il file di output: {ex.Message}", LogLevel.ERROR);
            Console.WriteLine($"Errore: Impossibile creare/scrivere nel file di output: {ex.Message}");
            return false;
        }
    }

    private static void MergeFiles(List<string> filesToProcess, CombineFilesOptions options, Logger logger)
    {
        // Ottieni info dettagliate sui file (token stimati)
        var fileCollector = new FileCollector(
            logger,
            options.IncludePaths ?? new List<string>(),
            options.ExcludePaths ?? new List<string>(),
            options.ExcludeFiles ?? new List<string>(),
            options.ExcludeFilePatterns ?? new List<string>()
        );
        var fileInfos = filesToProcess
            .Select(f => new { Path = f, Info = GetFileInfoSafe(fileCollector, f) })
            .Where(x => x.Info != null)
            .Select(x => x.Info)
            .OrderBy(f => f.EstimatedTokens) // puoi cambiare strategia qui
            .ToList();

        // Usa PaginatedFileMerger se la strategia è PaginateOutput
        if (options.PartialFileMode == TokenLimitStrategy.PaginateOutput && options.MaxTokensPerPage > 0)
        {
            using var paginatedMerger = new PaginatedFileMerger(
                logger,
                options.OutputToConsole,
                options.OutputFile,
                options.ListOnlyFileNames,
                options.MaxLinesPerFile,
                options.MaxTokensPerPage,
                options.MaxTotalTokens > 0 ? options.MaxTotalTokens : 0);

            AnsiConsole.Progress()
                .Start(ctx =>
                {
                    var progressTask = ctx.AddTask("[green]Merging files (paginated)...[/]", maxValue: fileInfos.Count);
                    foreach (var file in fileInfos)
                    {
                        paginatedMerger.MergeFile(file.Path);
                        progressTask.Increment(1);
                    }
                });

            paginatedMerger.FinalizePages();

            if (!options.OutputToConsole)
            {
                logger.WriteLog($"Operazione completata. Controlla i file paginati e l'indice.", LogLevel.INFO);
                Console.WriteLine($"Operazione completata. Controlla i file paginati e l'indice.");
            }
        }
        else
        {
            int budget = options.MaxTotalTokens > 0 ? options.MaxTotalTokens : int.MaxValue;
            using var fileMerger = new FileMerger(
                logger,
                options.OutputToConsole,
                options.OutputFile,
                options.ListOnlyFileNames,
                options.MaxLinesPerFile,
                options.PartialFileMode,
                0 // default, usiamo overload con limite per-file
            );

            AnsiConsole.Progress()
                .Start(ctx =>
                {
                    var progressTask = ctx.AddTask("[green]Merging files...[/]", maxValue: fileInfos.Count);
                    foreach (var file in fileInfos)
                    {
                        if (budget <= 0)
                            break;
                        int tokensForThisFile = file.EstimatedTokens;
                        if (tokensForThisFile <= budget)
                        {
                            fileMerger.MergeFile(file.Path);
                            budget -= tokensForThisFile;
                        }
                        else
                        {
                            // Tronca solo questo file
                            if (options.PartialFileMode == TokenLimitStrategy.IncludePartial)
                            {
                                fileMerger.MergeFile(file.Path, budget);
                                budget = 0;
                            }
                            // Escludi completamente se ExcludeCompletely
                            break;
                        }
                        progressTask.Increment(1);
                    }
                });

            if (!options.OutputToConsole)
            {
                logger.WriteLog($"Operazione completata. Controlla il file '{options.OutputFile}'.", LogLevel.INFO);
                Console.WriteLine($"Operazione completata. Controlla il file '{options.OutputFile}'.");
            }
            else
            {
                logger.WriteLog("Operazione completata con output a console.", LogLevel.INFO);
                Console.WriteLine("Operazione completata con output a console.");
            }
        }
    }

    // Helper per ottenere CollectedFileInfo anche se il file non esiste più
    private static CollectedFileInfo GetFileInfoSafe(FileCollector collector, string path)
    {
        try
        {
            var fi = new System.IO.FileInfo(path);
            return new CollectedFileInfo
            {
                Path = path,
                Size = fi.Length,
                LastModified = fi.LastWriteTime,
                Extension = fi.Extension.ToLower(),
                EstimatedTokens = collector.GetType()
                    .GetMethod("EstimateFileTokens", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Invoke(collector, new object[] { path, fi }) as int? ?? 1
            };
        }
        catch { return null; }
    }

    #endregion
}