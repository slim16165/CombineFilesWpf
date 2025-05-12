using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.Core.Services;

public class FileMerger
{
    private readonly Logger _logger;
    private readonly bool _outputToConsole;
    private readonly string? _outputFile;
    private readonly HashSet<string> _processedHashes = new(StringComparer.OrdinalIgnoreCase);
    private bool _listOnlyFileNames;

    public FileMerger(Logger logger, bool outputToConsole, string? outputFile, bool listOnlyFileNames)
    {
        _logger = logger;
        _outputToConsole = outputToConsole;
        _outputFile = outputFile;
        _listOnlyFileNames = listOnlyFileNames;
    }

    /// <summary>
    /// Stampa in console oppure scrive su file.
    /// </summary>
    private void WriteOutputOrFile(string content)
    {
        if (_outputToConsole)
        {
            Console.WriteLine(content);
        }
        else
        {
            File.AppendAllText(_outputFile, content + Environment.NewLine);
        }
    }

    public void MergeFile(string filePath)
    {
        try
        {
            string baseDir = Directory.GetCurrentDirectory();
            // Calcola il percorso relativo
            string relativePath = FileHelper.GetRelativePath(baseDir, filePath);

            // Costruisci l’intestazione usando la subfolder

            string header = _listOnlyFileNames
                ? $"### {relativePath} ###"
                : $"### Contenuto di {relativePath} ###";
            WriteOutputOrFile(header);

            //Debugger.Break();


            if (!_listOnlyFileNames)
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                    WriteOutputOrFile(line);
                // Riga vuota di separazione
                WriteOutputOrFile(string.Empty);
            }
        }
        catch (Exception ex)
        {
            // In caso di errore, includi sempre il relativePath per chiarezza
            string baseDir = Directory.GetCurrentDirectory();
            string relativePath = FileHelper.GetRelativePath(baseDir, filePath);
            WriteOutputOrFile($"[ERROR: impossibile leggere {relativePath} - {ex.Message}]");
            _logger.WriteLog($"Impossibile leggere il file: {filePath} - {ex.Message}", LogLevel.WARNING);
        }
    }

}