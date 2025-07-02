using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.Core.Services;

public class CollectedFileInfo
{
    public string Path { get; set; }
    public int EstimatedTokens { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public string Extension { get; set; }
}

public class FileCollectionResult
{
    public List<CollectedFileInfo> IncludedFiles { get; set; } = new();
    public List<CollectedFileInfo> ExcludedFiles { get; set; } = new();
    public int TotalTokens { get; set; }
    public int MaxTokens { get; set; }
    public bool TokenLimitReached => MaxTokens > 0 && ExcludedFiles.Count > 0;
}

public enum FilePriorityStrategy
{
    SizeAscending,      // Prima i file piccoli
    SizeDescending,     // Prima i file grandi
    DateNewest,         // Prima i più recenti
    DateOldest,         // Prima i più vecchi
    Extension,          // Raggruppa per estensione
    Alphabetical        // Ordine alfabetico
}

public class FileCollector
{
    private readonly Logger _logger;
    private readonly List<string> _excludePaths;
    private readonly List<string> _excludeFiles;
    private readonly List<string> _excludeFilePatterns;
    private readonly string _basePath;

    public FileCollector(
        Logger logger,
        List<string> excludePaths,
        List<string> excludeFiles,
        List<string> excludeFilePatterns)
    {
        _logger = logger;
        _excludePaths = excludePaths;
        _excludeFiles = excludeFiles;
        _excludeFilePatterns = excludeFilePatterns;
        _basePath = Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Converte un percorso assoluto in relativo rispetto a _basePath.
    /// Se la conversione fallisce, restituisce comunque il path originale.
    /// </summary>
    private string ToRelativePath(string fullPath)
    {
        try
        {
            return FileHelper.GetRelativePath(_basePath, fullPath);
        }
        catch
        {
            return fullPath;
        }
    }

    private bool IsPathExcluded(string filePath)
    {
        // Esclusione per directory
        foreach (var path in _excludePaths)
        {
            if (filePath.StartsWith(path, StringComparison.OrdinalIgnoreCase))
            {
                _logger.WriteLog(
                    $"Escluso per percorso: {ToRelativePath(filePath)} corrisponde a {ToRelativePath(path)}",
                    LogLevel.DEBUG
                );
                return true;
            }
        }

        // Esclusione per nome file
        string fileName = Path.GetFileName(filePath);
        foreach (var excludedFile in _excludeFiles)
        {
            if (fileName.Equals(excludedFile, StringComparison.OrdinalIgnoreCase))
            {
                _logger.WriteLog(
                    $"Escluso per nome file: {ToRelativePath(filePath)} corrisponde a {excludedFile}",
                    LogLevel.DEBUG
                );
                return true;
            }
        }

        // Esclusione per pattern regex
        foreach (var pattern in _excludeFilePatterns)
        {
            if (Regex.IsMatch(filePath, pattern))
            {
                _logger.WriteLog(
                    $"Escluso per pattern regex: {ToRelativePath(filePath)} corrisponde a {pattern}",
                    LogLevel.DEBUG
                );
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Ricorsivamente raccoglie tutti i file a partire da un percorso (compatibilità con vecchio metodo).
    /// </summary>
    public List<string> GetAllFiles(string startPath, bool recurse)
    {
        var result = GetAllFilesWithTokenInfo(startPath, recurse, null);
        return result.IncludedFiles.Select(f => f.Path).ToList();
    }

    /// <summary>
    /// Ricorsivamente raccoglie tutti i file a partire da un percorso, con limite opzionale di token (compatibilità).
    /// </summary>
    public List<string> GetAllFiles(string startPath, bool recurse, int? maxTokens)
    {
        var result = GetAllFilesWithTokenInfo(startPath, recurse, maxTokens);
        return result.IncludedFiles.Select(f => f.Path).ToList();
    }

    /// <summary>
    /// Raccoglie tutti i file con informazioni dettagliate sui token.
    /// </summary>
    public FileCollectionResult GetAllFilesWithTokenInfo(
        string startPath,
        bool recurse,
        int? maxTokens,
        FilePriorityStrategy priorityStrategy = FilePriorityStrategy.SizeAscending,
        List<string> priorityFiles = null)
    {
        var allFiles = new List<CollectedFileInfo>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Prima raccogli tutti i file
        CollectAllFiles(startPath, recurse, allFiles, visited);

        // Poi applica la strategia di selezione con il limite token
        return SelectFilesWithinTokenLimit(allFiles, maxTokens, priorityStrategy, priorityFiles);
    }

    private void CollectAllFiles(string currentPath, bool recurse, List<CollectedFileInfo> allFiles, HashSet<string> visited)
    {
        if (!visited.Add(currentPath))
            return;

        string[] items;
        try
        {
            items = Directory.GetFileSystemEntries(currentPath);
        }
        catch (Exception ex)
        {
            _logger.WriteLog(
                $"Errore durante l'accesso al percorso: {ToRelativePath(currentPath)} - {ex.Message}",
                LogLevel.WARNING);
            return;
        }

        foreach (var item in items)
        {
            if (IsPathExcluded(item))
            {
                _logger.WriteLog($"Percorso o file escluso: {ToRelativePath(item)}", LogLevel.DEBUG);
                continue;
            }

            if (Directory.Exists(item))
            {
                if (recurse)
                {
                    var attr = File.GetAttributes(item);
                    bool isReparse = (attr & FileAttributes.ReparsePoint) != 0;
                    if (isReparse)
                    {
                        _logger.WriteLog(
                            $"Trovato reparse point: {ToRelativePath(item)} -- non risolto",
                            LogLevel.DEBUG
                        );
                    }
                    else
                    {
                        CollectAllFiles(item, recurse, allFiles, visited);
                    }
                }
            }
            else
            {
                try
                {
                    var normalized = FileHelper.NormalizeLongPath(item);
                    var fi = new System.IO.FileInfo(normalized);

                    allFiles.Add(new CollectedFileInfo
                    {
                        Path = normalized,
                        Size = fi.Length,
                        LastModified = fi.LastWriteTime,
                        Extension = fi.Extension.ToLower(),
                        EstimatedTokens = EstimateFileTokens(normalized, fi)
                    });
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(
                        $"Errore nell'accesso al file: {ToRelativePath(item)} - {ex.Message}",
                        LogLevel.WARNING);
                }
            }
        }
    }

    private FileCollectionResult SelectFilesWithinTokenLimit(
        List<CollectedFileInfo> allFiles,
        int? maxTokens,
        FilePriorityStrategy priorityStrategy,
        List<string> priorityFiles)
    {
        var result = new FileCollectionResult { MaxTokens = maxTokens ?? 0 };

        if (!maxTokens.HasValue)
        {
            result.IncludedFiles = allFiles;
            result.TotalTokens = allFiles.Sum(f => f.EstimatedTokens + EstimateOutputOverhead(f));
            return result;
        }

        // Separa i file prioritari se specificati
        var priorityFileSet = new HashSet<string>(priorityFiles ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        var highPriorityFiles = new List<CollectedFileInfo>();
        var normalFiles = new List<CollectedFileInfo>();

        foreach (var file in allFiles)
        {
            if (priorityFileSet.Contains(file.Path))
                highPriorityFiles.Add(file);
            else
                normalFiles.Add(file);
        }

        // Ordina i file normali secondo la strategia
        var sortedFiles = SortFilesByStrategy(normalFiles, priorityStrategy);

        // Prima aggiungi i file prioritari
        foreach (var file in highPriorityFiles)
        {
            int totalTokensWithFile = result.TotalTokens + file.EstimatedTokens + EstimateOutputOverhead(file);

            if (totalTokensWithFile <= maxTokens.Value)
            {
                result.IncludedFiles.Add(file);
                result.TotalTokens = totalTokensWithFile;
            }
            else
            {
                result.ExcludedFiles.Add(file);
                _logger.WriteLog(
                    $"File prioritario escluso per limite token: {ToRelativePath(file.Path)} ({file.EstimatedTokens} token)",
                    LogLevel.WARNING);
            }
        }

        // Poi aggiungi i file normali
        foreach (var file in sortedFiles)
        {
            int totalTokensWithFile = result.TotalTokens + file.EstimatedTokens + EstimateOutputOverhead(file);

            if (totalTokensWithFile <= maxTokens.Value)
            {
                result.IncludedFiles.Add(file);
                result.TotalTokens = totalTokensWithFile;
            }
            else
            {
                result.ExcludedFiles.Add(file);
            }
        }

        // Log del risultato
        _logger.WriteLog(
            $"Raccolta file completata: {result.IncludedFiles.Count} inclusi ({result.TotalTokens} token), " +
            $"{result.ExcludedFiles.Count} esclusi per limite token",
            LogLevel.INFO);

        if (result.ExcludedFiles.Count > 0)
        {
            _logger.WriteLog(
                $"Spazio token utilizzato: {result.TotalTokens}/{maxTokens.Value} " +
                $"({(result.TotalTokens * 100.0 / maxTokens.Value):F1}%)",
                LogLevel.INFO);
        }

        return result;
    }

    private List<CollectedFileInfo> SortFilesByStrategy(List<CollectedFileInfo> files, FilePriorityStrategy strategy)
    {
        return strategy switch
        {
            FilePriorityStrategy.SizeAscending => files.OrderBy(f => f.Size).ToList(),
            FilePriorityStrategy.SizeDescending => files.OrderByDescending(f => f.Size).ToList(),
            FilePriorityStrategy.DateNewest => files.OrderByDescending(f => f.LastModified).ToList(),
            FilePriorityStrategy.DateOldest => files.OrderBy(f => f.LastModified).ToList(),
            FilePriorityStrategy.Extension => files.OrderBy(f => f.Extension).ThenBy(f => f.Path).ToList(),
            FilePriorityStrategy.Alphabetical => files.OrderBy(f => f.Path).ToList(),
            _ => files
        };
    }

    private int EstimateOutputOverhead(CollectedFileInfo file)
    {
        // Stima token per header, separatori, etc.
        // Es: "=== filename.txt ===" + newlines + path info
        string relativePath = ToRelativePath(file.Path);
        int headerLength = relativePath.Length + 30; // margine per separatori e newline
        return (int)Math.Ceiling(headerLength / 4.0);
    }

    private int EstimateFileTokens(string filePath, System.IO.FileInfo fileInfo)
    {
        try
        {
            // Usa una stima più accurata basata sul tipo di file
            string extension = fileInfo.Extension.ToLower();
            double tokensPerByte = extension switch
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

            // Per file molto piccoli, usa un minimo di token
            int estimatedTokens = (int)Math.Ceiling(fileInfo.Length * tokensPerByte);
            return Math.Max(estimatedTokens, 1);
        }
        catch (Exception ex)
        {
            _logger.WriteLog(
                $"Errore nella stima dei token per {ToRelativePath(filePath)}: {ex.Message}",
                LogLevel.DEBUG);
            return 1; // Ritorna almeno 1 token
        }
    }

    /// <summary>
    /// Avvia la selezione interattiva tramite Notepad con informazioni sui token.
    /// </summary>
    public List<string> StartInteractiveSelection(List<string> initialFiles, string sourcePath)
    {
        var fileInfos = new List<CollectedFileInfo>();

        // Raccogli informazioni sui file
        foreach (var file in initialFiles)
        {
            try
            {
                var normalized = FileHelper.NormalizeLongPath(file);
                var fi = new System.IO.FileInfo(normalized);
                fileInfos.Add(new CollectedFileInfo
                {
                    Path = file,
                    Size = fi.Length,
                    EstimatedTokens = EstimateFileTokens(normalized, fi),
                    Extension = fi.Extension
                });
            }
            catch
            {
                // Skip file con errori
            }
        }

        // Prepara il contenuto con info sui token
        var lines = new List<string>();
        lines.Add("# File da includere nella combinazione");
        lines.Add("# Rimuovi le righe dei file che NON vuoi includere");
        lines.Add($"# Token totali stimati: {fileInfos.Sum(f => f.EstimatedTokens + EstimateOutputOverhead(f))}");
        lines.Add("#");

        foreach (var fileInfo in fileInfos)
        {
            try
            {
                var relativePath = FileHelper.GetRelativePath(sourcePath, fileInfo.Path);
                var sizeKb = fileInfo.Size / 1024.0;
                lines.Add($"{relativePath} # {sizeKb:F1}KB, ~{fileInfo.EstimatedTokens} token");
            }
            catch
            {
                lines.Add(fileInfo.Path);
            }
        }

        string tempFilePath = Path.Combine(Path.GetTempPath(), "CombineFiles_InteractiveSelection.txt");

        try
        {
            File.WriteAllLines(tempFilePath, lines, Encoding.UTF8);
            _logger.WriteLog($"File di configurazione temporaneo creato: {tempFilePath}", LogLevel.DEBUG);
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Errore nella scrittura del file temporaneo: {ex.Message}", LogLevel.ERROR);
            return new List<string>();
        }

        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "notepad.exe";
            process.StartInfo.Arguments = tempFilePath;
            process.StartInfo.UseShellExecute = true;
            process.Start();
            process.WaitForExit();

            _logger.WriteLog("Editor chiuso. Lettura del file di configurazione aggiornato.", LogLevel.DEBUG);
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Errore nell'apertura di notepad: {ex.Message}", LogLevel.ERROR);
            return new List<string>();
        }

        List<string> updatedRelativePaths;
        try
        {
            updatedRelativePaths = File.ReadAllLines(tempFilePath, Encoding.UTF8)
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#"))
                .Select(l => l.Split('#')[0].Trim()) // Rimuovi commenti
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Errore nella lettura del file temporaneo: {ex.Message}", LogLevel.ERROR);
            return new List<string>();
        }

        var updatedFiles = new List<string>();
        foreach (var rel in updatedRelativePaths)
        {
            string absPath = Path.Combine(sourcePath, rel);
            if (File.Exists(absPath))
            {
                updatedFiles.Add(FileHelper.NormalizeLongPath(absPath));
            }
            else
            {
                _logger.WriteLog($"File non trovato durante la lettura del config: {absPath}", LogLevel.WARNING);
            }
        }

        _logger.WriteLog($"File aggiornati dopo InteractiveSelection: {updatedFiles.Count}", LogLevel.DEBUG);

        try
        {
            File.Delete(tempFilePath);
        }
        catch
        {
            /* Ignora errori di cancellazione */
        }

        return updatedFiles;
    }

    /// <summary>
    /// Applica il limite token a una lista di CollectedFileInfo già filtrata e restituisce solo i path inclusi.
    /// </summary>
    public List<string> ApplyTokenLimitToFilteredFiles(List<CollectedFileInfo> filteredFiles, int maxTokens, FilePriorityStrategy priorityStrategy = FilePriorityStrategy.SizeAscending)
    {
        var result = SelectFilesWithinTokenLimit(filteredFiles, maxTokens, priorityStrategy, null);
        return result.IncludedFiles.Select(f => f.Path).ToList();
    }
}