using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.Core.Services;

/// <summary>
/// Unisce (o elenca) i file specificati, scrivendo l'output su console oppure su file.
/// – Gestisce opzionalmente:
///   • un limite massimo di righe da includere per ciascun file (<paramref name="maxLinesPerFile"/>)
///   • un callback esterno che decide se proseguire in base al “costo” in token della riga
///     (<paramref name="tokenBudgetCallback"/>).<br/>
///   In assenza di tali vincoli, il comportamento rimane identico a prima (caso standard).
/// </summary>
public sealed class FileMerger : IDisposable
{
    private readonly Logger _logger;
    private readonly bool _outputToConsole;
    private readonly StreamWriter? _writer;
    private readonly bool _listOnlyFileNames;
    private readonly int _maxLinesPerFile;
    private readonly Func<int, bool>? _tokenBudgetCallback; // true = puoi consumare, false = stop
    private readonly HashSet<string> _processedHashes = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _baseDir = Directory.GetCurrentDirectory();
    private readonly SHA256 _sha = SHA256.Create();

    /// <param name="logger">Logger applicativo.</param>
    /// <param name="outputToConsole">True → scrive su console, false → scrive su <paramref name="outputFile"/>.</param>
    /// <param name="outputFile">File di destinazione (obbligatorio se <paramref name="outputToConsole"/> è false).</param>
    /// <param name="listOnlyFileNames">True → stampa solo i nomi dei file, senza contenuto.</param>
    /// <param name="maxLinesPerFile">0 = nessun limite di righe; &gt;0 = considera solo le prime N righe.</param>
    /// <param name="tokenBudgetCallback">
    ///     Callback esterna che riceve il numero di token della riga corrente.<br/>
    ///     Deve restituire <c>true</c> se la riga può essere scritta, <c>false</c> se il budget è esaurito
    ///     (in tal caso il merge si interrompe immediatamente).<br/>
    ///     Se <c>null</c>, il budget non viene considerato.
    /// </param>
    public FileMerger(
        Logger logger,
        bool outputToConsole,
        string? outputFile,
        bool listOnlyFileNames,
        int maxLinesPerFile = 0,
        Func<int, bool>? tokenBudgetCallback = null)
    {
        _logger = logger;
        _outputToConsole = outputToConsole;
        _listOnlyFileNames = listOnlyFileNames;
        _maxLinesPerFile = maxLinesPerFile;
        _tokenBudgetCallback = tokenBudgetCallback;

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
            Console.WriteLine(line);
        else
            _writer!.WriteLine(line);
    }

    /// <summary>
    /// Unisce (o elenca) un singolo file.
    /// </summary>
    /// <param name="filePath">Percorso assoluto del file.</param>
    /// <param name="avoidDuplicatesByHash">Se true salta i file già processati (con lo stesso hash di contenuto).</param>
    /// <returns>
    ///     <c>true</c> se il file è stato processato interamente;<br/>
    ///     <c>false</c> se ci si è fermati per superamento budget token (richiamante può interrompere il ciclo).
    /// </returns>
    public bool MergeFile(string filePath, bool avoidDuplicatesByHash = true)
    {
        try
        {
            if (avoidDuplicatesByHash && IsDuplicate(filePath))
            {
                _logger.WriteLog($"Skipped duplicate: {filePath}", LogLevel.DEBUG);
                return true;
            }

            var relativePath = FileHelper.GetRelativePath(_baseDir, filePath);
            WriteLine(_listOnlyFileNames
                ? $"### {relativePath} ###"
                : $"### Contenuto di {relativePath} ###");

            if (_listOnlyFileNames)
                return true;

            int lineIdx = 0;
            foreach (var line in File.ReadLines(filePath))
            {
                // 1. MaxLinesPerFile
                if (_maxLinesPerFile > 0 && lineIdx >= _maxLinesPerFile)
                    break;

                // 2. Budget token (se usato)
                if (_tokenBudgetCallback is not null)
                {
                    int tokensInLine = CountTokens(line);
                    if (!_tokenBudgetCallback(tokensInLine))
                    {
                        _logger.WriteLog("Budget token esaurito: interrompo il merge.", LogLevel.INFO);
                        return false; // interrompe l'intero processo
                    }
                }

                WriteLine(line);
                lineIdx++;
            }

            WriteLine(); // Riga vuota di separazione
            return true;
        }
        catch (Exception ex)
        {
            var relativePath = FileHelper.GetRelativePath(_baseDir, filePath);
            WriteLine($"[ERROR: impossibile leggere {relativePath} - {ex.Message}]");
            _logger.WriteLog($"Impossibile leggere il file: {filePath} - {ex.Message}", LogLevel.WARNING);
            return true;
        }
    }

    /// <summary>Conta i “token” in maniera approssimativa come numero di parole.</summary>
    private static int CountTokens(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return 0;

        return line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>Calcola lo SHA-256 del file per evitare duplicati.</summary>
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
