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
    }

    private bool IsPathExcluded(string filePath)
    {
        // Esclusione per directory
        foreach (var path in _excludePaths)
        {
            if (filePath.StartsWith(path, StringComparison.OrdinalIgnoreCase))
            {
                _logger.WriteLog($"Escluso per percorso: {filePath} corrisponde a {path}", "DEBUG");
                return true;
            }
        }

        // Esclusione per nome file
        string fileName = Path.GetFileName(filePath);
        foreach (var excludedFile in _excludeFiles)
        {
            if (fileName.Equals(excludedFile, StringComparison.OrdinalIgnoreCase))
            {
                _logger.WriteLog($"Escluso per nome file: {filePath} corrisponde a {excludedFile}", "DEBUG");
                return true;
            }
        }

        // Esclusione per pattern regex
        foreach (var pattern in _excludeFilePatterns)
        {
            if (Regex.IsMatch(filePath, pattern))
            {
                _logger.WriteLog($"Escluso per pattern regex: {filePath} corrisponde a {pattern}", "DEBUG");
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
        var result = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void RecursiveGetFiles(string currentPath)
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
                _logger.WriteLog($"Errore durante l'accesso al percorso: {currentPath} - {ex.Message}", "WARNING");
                return;
            }

            foreach (var item in items)
            {
                if (IsPathExcluded(item))
                {
                    _logger.WriteLog($"Percorso o file escluso: {item}", "DEBUG");
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
                            _logger.WriteLog($"Trovato reparse point: {item} – non risolto", "DEBUG");
                        }
                        RecursiveGetFiles(item);
                    }
                }
                else
                {
                    _logger.WriteLog($"File incluso: {item}", "DEBUG");
                    result.Add(item);
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
        var relativePaths = initialFiles.Select(file =>
        {
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
        try
        {
            File.WriteAllLines(tempFilePath, (IEnumerable<string>)relativePaths, Encoding.UTF8);
            _logger.WriteLog($"File di configurazione temporaneo creato: {tempFilePath}", "DEBUG");
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Errore nella scrittura del file temporaneo: {ex.Message}", "ERROR");
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

            _logger.WriteLog("Editor chiuso. Lettura del file di configurazione aggiornato.", "DEBUG");
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Errore nell'apertura di notepad: {ex.Message}", "ERROR");
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
            _logger.WriteLog($"Errore nella lettura del file temporaneo: {ex.Message}", "ERROR");
            return new List<string>();
        }

        var updatedFiles = new List<string>();
        foreach (var rel in updatedRelativePaths)
        {
            string absPath = Path.Combine(sourcePath, rel);
            if (File.Exists(absPath))
            {
                updatedFiles.Add(absPath);
            }
            else
            {
                _logger.WriteLog($"File non trovato durante la lettura del config: {absPath}", "WARNING");
            }
        }
        _logger.WriteLog($"File aggiornati dopo InteractiveSelection: {updatedFiles.Count}", "DEBUG");

        try
        {
            File.Delete(tempFilePath);
        }
        catch { /* Ignora errori di cancellazione */ }

        return updatedFiles;
    }
}