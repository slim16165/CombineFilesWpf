using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using CombineFiles.ConsoleApp.Infrastructure;

namespace CombineFiles.ConsoleApp.Core;

public class FileMerger
{
    private readonly Logger _logger;
    private readonly bool _outputToConsole;
    private readonly string? _outputFile;
    private readonly string _outputFormat;
    private readonly bool _fileNamesOnly;
    private readonly HashSet<string> _processedHashes = new(StringComparer.OrdinalIgnoreCase);

    public FileMerger(Logger logger, bool outputToConsole, string? outputFile, string outputFormat, bool fileNamesOnly)
    {
        _logger = logger;
        _outputToConsole = outputToConsole;
        _outputFile = outputFile;
        _outputFormat = outputFormat;
        _fileNamesOnly = fileNamesOnly;
    }

    /// <summary>
    /// Stampa in console oppure scrive su file.
    /// </summary>
    public void WriteOutputOrFile(string content)
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

    /// <summary>
    /// Aggiunge il contenuto dei file alla destinazione.
    /// </summary>
    public void MergeFiles(List<string> files)
    {
        foreach (var filePath in files)
        {
            // Calcola l’hash SHA256 per evitare duplicati (hard link)
            string hashString;
            try
            {
                using var sha256 = SHA256.Create();
                using var fs = File.OpenRead(filePath);
                var hash = sha256.ComputeHash(fs);
                hashString = BitConverter.ToString(hash).Replace("-", "");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"Attenzione: Impossibile calcolare l'hash del file: {filePath}");
                Console.ResetColor();

                _logger.WriteLog($"Impossibile calcolare l'hash del file: {filePath} - {ex.Message}", "WARNING");
                continue;
            }

            if (_processedHashes.Contains(hashString))
            {
                _logger.WriteLog($"File già processato (hard link): {filePath}", "DEBUG");
                continue;
            }
            _processedHashes.Add(hashString);

            // Aggiunge intestazione
            string fileName = Path.GetFileName(filePath);
            string header = _fileNamesOnly
                ? $"### {fileName} ###"
                : $"### Contenuto di {fileName} ###";
            WriteOutputOrFile(header);

            if (!_fileNamesOnly)
            {
                _logger.WriteLog($"Aggiungendo contenuto di: {fileName}", "INFO");

                try
                {
                    var lines = File.ReadAllLines(filePath);
                    if (_outputToConsole)
                    {
                        foreach (var line in lines)
                            Console.WriteLine(line);
                        Console.WriteLine();
                    }
                    else
                    {
                        File.AppendAllLines(_outputFile, lines);
                        File.AppendAllText(_outputFile, Environment.NewLine);
                    }
                    _logger.WriteLog($"File aggiunto correttamente: {fileName}", "INFO");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"Attenzione: Impossibile leggere il file: {filePath}");
                    Console.ResetColor();

                    _logger.WriteLog($"Impossibile leggere il file: {filePath} - {ex.Message}", "WARNING");
                }
            }
        }
    }
}