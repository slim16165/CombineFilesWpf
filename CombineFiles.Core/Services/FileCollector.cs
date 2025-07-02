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

public class FileCollector
{
    private readonly Logger _logger;
    private readonly List<string> _excludePaths;
    private readonly List<string> _excludeFiles;
    private readonly List<string> _excludeFilePatterns;

    // Base path da usare per i percorsi relativi (puoi usare anche un costruttore
    // che riceve la cartella di partenza esplicitamente).
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

        // Ad esempio, la cartella di partenza potrebbe essere l'ultima
        // dove hai lanciato l’app o un parametro in CombineFilesOptions.
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
                // Log di debug (non comparirà se MinimumLogLevel > DEBUG)
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
    /// Ricorsivamente raccoglie tutti i file a partire da un percorso.
    /// </summary>
    public List<string> GetAllFiles(string startPath, bool recurse)
    {
        // NON normalizzare con long path qui: le API directory .NET 4.7.2 non supportano \\?\
        var result = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void RecursiveGetFiles(string currentPath)
        {
            // NON normalizzare con long path qui
            if (!visited.Add(currentPath))
                return;

            string[] items;
            try
            {
                items = Directory.GetFileSystemEntries(currentPath);
            }
            catch (Exception ex)
            {
                // In output normale, la segnali come WARNING
                _logger.WriteLog(
                    $"Errore durante l'accesso al percorso: {ToRelativePath(currentPath)} - {ex.Message}",
                    LogLevel.WARNING);
                return;
            }

            foreach (var item in items)
            {
                // NON normalizzare con long path qui
                if (IsPathExcluded(item))
                {
                    // Log di debug. I dettagli li scrivo a livello debug.
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
                                $"Trovato reparse point: {ToRelativePath(item)} – non risolto",
                                LogLevel.DEBUG
                            );
                        }

                        RecursiveGetFiles(item);
                    }
                }
                else
                {
                    // Solo qui, se il path è lungo, normalizza per file
                    result.Add(FileHelper.NormalizeLongPath(item));
                }
            }
        }

        RecursiveGetFiles(startPath);
        return result;
    }

    /// <summary>
    /// Avvia la selezione interattiva tramite Notepad.
    /// </summary>
    public List<string> StartInteractiveSelection(List<string> initialFiles, string sourcePath)
    {
        // NON normalizzare sourcePath qui
        var relativePaths = initialFiles.Select(file =>
        {
            // Normalizza solo se serve per file
            try
            {
                return FileHelper.GetRelativePath(sourcePath, file);
            }
            catch
            {
                return file;
            }
        }).ToList();

        string tempFilePath = Path.Combine(Path.GetTempPath(), "CombineFiles_InteractiveSelection.txt");
        // NON normalizzare tempFilePath qui
        try
        {
            File.WriteAllLines(tempFilePath, (IEnumerable<string>)relativePaths, Encoding.UTF8);
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
                .Where(l => !string.IsNullOrWhiteSpace(l))
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
            // Normalizza solo se serve per file
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
}