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
    /// <summary>
    /// Unisce (o elenca) il file <paramref name="filePath"/> nel flusso di output.
    /// 
    /// Comportamenti opzionali:
    /// • Evita i duplicati (hash SHA‑256) se <paramref name="avoidDuplicatesByHash"/> è <c>true</c>.
    /// • Limita le righe o rispetta il budget token se queste feature sono state abilitate
    ///   tramite il costruttore.
    /// </summary>
    /// <returns>
    /// <c>true</c> se si può proseguire con i file successivi; <c>false</c> se il
    /// budget token è esaurito e va interrotto l’intero processo.
    /// </returns>
    public bool MergeFile(string filePath, bool avoidDuplicatesByHash = true)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath cannot be null or empty.", nameof(filePath));

        // 1. Evita duplicati (opzionale)
        if (avoidDuplicatesByHash && IsDuplicate(filePath))
        {
            LogSkipDuplicate(filePath);
            return true;
        }

        // 2. Header – sempre visibile, anche con _listOnlyFileNames
        var relativePath = FileHelper.GetRelativePath(_baseDir, filePath);
        WriteHeader(relativePath);

        // Modalità "list only": stampa solo il nome e termina.
        if (_listOnlyFileNames)
            return true;

        // 3. Corpo – eventualmente interrotto da budget token o maxLinesPerFile
        bool continueProcessing = ProcessFileLines(filePath);

        // 4. Riga vuota separatrice + flush esplicito (importante se il chiamante
        //    dimentica di chiamare Dispose())
        WriteLine();
        Flush();

        return continueProcessing;
    }

    #region Helper privati

    private void WriteHeader(string relativePath)
        => WriteLine(_listOnlyFileNames ? $"### {relativePath} ###" : $"### Contenuto di {relativePath} ###");

    private void LogSkipDuplicate(string filePath)
        => _logger.WriteLog($"Skipped duplicate: {filePath}", LogLevel.DEBUG);

    private bool ProcessFileLines(string filePath)
    {
        filePath = FileHelper.NormalizeLongPath(filePath);
        int lineIdx = 0;
        foreach (var line in File.ReadLines(filePath))
        {
            // a) limite righe per file (opz.)
            if (ExceededMaxLines(lineIdx))
                break;

            // b) budget token (opz.)
            if (TokenBudgetExceeded(line))
                return false; // interrompe l’intero merge

            WriteLine(line);
            lineIdx++;
        }

        return true; // ok, si può continuare con i file successivi
    }

    private bool ExceededMaxLines(int lineIdx)
        => _maxLinesPerFile > 0 && lineIdx >= _maxLinesPerFile;

    private bool TokenBudgetExceeded(string line)
    {
        if (_tokenBudgetCallback is null)
            return false;

        int tokens = CountTokens(line);
        bool allowed = _tokenBudgetCallback(tokens);
        if (!allowed)
            _logger.WriteLog("Budget token esaurito: interrompo il merge.", LogLevel.INFO);

        return !allowed;
    }

    /// <summary>Flush esplicito: evita file troncati se il chiamante non chiama Dispose().</summary>
    private void Flush() => _writer?.Flush();

    #endregion

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
        filePath = FileHelper.NormalizeLongPath(filePath);
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
