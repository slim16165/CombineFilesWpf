using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.Core.Services;

/// <summary>
/// Unisce (o semplicemente elenca) i file specificati, scrivendo l'output
/// su console oppure su file. L'istanza mantiene un unico StreamWriter
/// aperto per tutta la durata dell'operazione, riducendo drasticamente il
/// numero di handle e di scansioni antivirus rispetto a File.AppendAllText.
/// </summary>
public sealed class FileMerger : IDisposable
{
    private readonly Logger _logger;
    private readonly bool _outputToConsole;
    private readonly StreamWriter? _writer;
    private readonly bool _listOnlyFileNames;
    private readonly HashSet<string> _processedHashes = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _baseDir = Directory.GetCurrentDirectory();
    private readonly SHA256 _sha = SHA256.Create();

    /// <param name="logger">Logger applicativo.</param>
    /// <param name="outputToConsole">True = scrive su console, false = scrive su <paramref name="outputFile"/>.</param>
    /// <param name="outputFile">Percorso del file di output (richiesto se <paramref name="outputToConsole"/> è false).</param>
    /// <param name="listOnlyFileNames">Se true stampa solo i nomi dei file, senza contenuto.</param>
    public FileMerger(Logger logger,
                      bool outputToConsole,
                      string? outputFile,
                      bool listOnlyFileNames)
    {
        _logger = logger;
        _outputToConsole = outputToConsole;
        _listOnlyFileNames = listOnlyFileNames;

        if (!_outputToConsole)
        {
            if (string.IsNullOrWhiteSpace(outputFile))
                throw new ArgumentException("outputFile cannot be null when outputToConsole is false", nameof(outputFile));

            // Apri il file una sola volta: meno I/O, meno scansioni antivirus.
            _writer = new StreamWriter(outputFile, append: false, encoding: new UTF8Encoding(false));
        }
    }

    /// <summary>Scrive una linea su console o su file.</summary>
    private void WriteLine(string line = "")
    {
        if (_outputToConsole)
        {
            Console.WriteLine(line);
        }
        else
        {
            _writer!.WriteLine(line);
        }
    }

    /// <summary>
    /// Unisce (o elenca) un singolo file.
    /// </summary>
    /// <param name="filePath">Percorso assoluto del file.</param>
    /// <param name="avoidDuplicatesByHash">Se true salta i file già processati (con lo stesso hash di contenuto).</param>
    public void MergeFile(string filePath, bool avoidDuplicatesByHash = true)
    {
        try
        {
            if (avoidDuplicatesByHash && IsDuplicate(filePath))
            {
                _logger.WriteLog($"Skipped duplicate: {filePath}", LogLevel.DEBUG);
                return;
            }

            var relativePath = FileHelper.GetRelativePath(_baseDir, filePath);
            WriteLine(_listOnlyFileNames
                ? $"### {relativePath} ###"
                : $"### Contenuto di {relativePath} ###");

            if (!_listOnlyFileNames)
            {
                foreach (var line in File.ReadLines(filePath))
                    WriteLine(line);

                // Riga vuota di separazione
                WriteLine();
            }
        }
        catch (Exception ex)
        {
            var relativePath = FileHelper.GetRelativePath(_baseDir, filePath);
            WriteLine($"[ERROR: impossibile leggere {relativePath} - {ex.Message}]");
            _logger.WriteLog($"Impossibile leggere il file: {filePath} - {ex.Message}", LogLevel.WARNING);
        }
    }

    /// <summary>
    /// Calcola lo SHA-256 del file per evitare duplicati.
    /// </summary>
    private bool IsDuplicate(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var hashBytes = _sha.ComputeHash(stream);
            var hashString = Convert.ToBase64String(hashBytes);
            return !_processedHashes.Add(hashString);
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Unable to compute hash for {filePath}: {ex.Message}", LogLevel.WARNING);
            return false;
        }
    }

    public void Dispose()
    {
        _writer?.Dispose();
        _sha.Dispose();
    }
}
