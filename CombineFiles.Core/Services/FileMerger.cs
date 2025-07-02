using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.Core.Services;

/// <summary>
/// Strategia per gestire i file quando si raggiunge il limite di token.
/// </summary>
public enum TokenLimitStrategy
{
    /// <summary>Esclude completamente i file che superano il limite.</summary>
    ExcludeComplete,

    /// <summary>Include parzialmente i file fino al limite.</summary>
    IncludePartial
}

/// <summary>
/// Informazioni sul troncamento di un file.
/// </summary>
public class FileTruncationInfo
{
    public long TotalBytes { get; set; }
    public long ProcessedBytes { get; set; }
    public int TotalLines { get; set; }
    public int ProcessedLines { get; set; }
    public double PercentageProcessed => TotalBytes > 0 ? (ProcessedBytes * 100.0 / TotalBytes) : 0;
    public bool WasTruncated => ProcessedBytes < TotalBytes;
}

/// <summary>
/// Unisce (o elenca) i file specificati, scrivendo l'output su console oppure su file.
/// -- Gestisce opzionalmente:
///   • un limite massimo di righe da includere per ciascun file (<paramref name="maxLinesPerFile"/>)
///   • un callback esterno che decide se proseguire in base al "costo" in token della riga
///     (<paramref name="tokenBudgetCallback"/>).<br/>
///   • una strategia per gestire i file quando si raggiunge il limite di token
///     (<paramref name="tokenLimitStrategy"/>).<br/>
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
    private readonly TokenLimitStrategy _tokenLimitStrategy;
    private readonly HashSet<string> _processedHashes = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _baseDir = Directory.GetCurrentDirectory();
    private readonly SHA256 _sha = SHA256.Create();

    // Stato per tracciare se il budget è esaurito globalmente
    private bool _globalTokenBudgetExhausted = false;

    /// <param name="logger">Logger applicativo.</param>
    /// <param name="outputToConsole">True → scrive su console, false → scrive su <paramref name="outputFile"/>.</param>
    /// <param name="outputFile">File di destinazione (obbligatorio se <paramref name="outputToConsole"/> è false).</param>
    /// <param name="listOnlyFileNames">True → stampa solo i nomi dei file, senza contenuto.</param>
    /// <param name="maxLinesPerFile">0 = nessun limite di righe; &gt;0 = considera solo le prime N righe.</param>
    /// <param name="tokenBudgetCallback">
    ///     Callback esterna che riceve il numero di token della riga corrente.<br/>
    ///     Deve restituire <c>true</c> se la riga può essere scritta, <c>false</c> se il budget è esaurito.<br/>
    ///     Se <c>null</c>, il budget non viene considerato.
    /// </param>
    /// <param name="tokenLimitStrategy">
    ///     Strategia da utilizzare quando si raggiunge il limite di token:<br/>
    ///     • <see cref="TokenLimitStrategy.ExcludeComplete"/> → esclude completamente i file successivi (comportamento legacy)<br/>
    ///     • <see cref="TokenLimitStrategy.IncludePartial"/> → include parzialmente i file fino al limite
    /// </param>
    public FileMerger(
        Logger logger,
        bool outputToConsole,
        string? outputFile,
        bool listOnlyFileNames,
        int maxLinesPerFile = 0,
        Func<int, bool>? tokenBudgetCallback = null,
        TokenLimitStrategy tokenLimitStrategy = TokenLimitStrategy.ExcludeComplete)
    {
        _logger = logger;
        _outputToConsole = outputToConsole;
        _listOnlyFileNames = listOnlyFileNames;
        _maxLinesPerFile = maxLinesPerFile;
        _tokenBudgetCallback = tokenBudgetCallback;
        _tokenLimitStrategy = tokenLimitStrategy;

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
    /// Unisce (o elenca) il file <paramref name="filePath"/> nel flusso di output.
    /// 
    /// Comportamenti opzionali:
    /// • Evita i duplicati (hash SHA‑256) se <paramref name="avoidDuplicatesByHash"/> è <c>true</c>.
    /// • Limita le righe o rispetta il budget token se queste feature sono state abilitate
    ///   tramite il costruttore.
    /// • Include parzialmente i file se la strategia è <see cref="TokenLimitStrategy.IncludePartial"/>.
    /// </summary>
    /// <returns>
    /// <c>true</c> se si può proseguire con i file successivi; <c>false</c> se il
    /// budget token è esaurito e la strategia è <see cref="TokenLimitStrategy.ExcludeComplete"/>.
    /// </returns>
    public bool MergeFile(string filePath, bool avoidDuplicatesByHash = true)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath cannot be null or empty.", nameof(filePath));

        // Se il budget è globalmente esaurito e la strategia è ExcludeComplete, non processare più nulla
        if (_globalTokenBudgetExhausted && _tokenLimitStrategy == TokenLimitStrategy.ExcludeComplete)
        {
            _logger.WriteLog($"Budget token esaurito globalmente, file ignorato: {filePath}", LogLevel.DEBUG);
            return false;
        }

        // 1. Evita duplicati (opzionale)
        if (avoidDuplicatesByHash && IsDuplicate(filePath))
        {
            LogSkipDuplicate(filePath);
            return true;
        }

        // 2. Header -- sempre visibile, anche con _listOnlyFileNames
        var relativePath = FileHelper.GetRelativePath(_baseDir, filePath);

        // Modalità "list only": stampa solo il nome e termina.
        if (_listOnlyFileNames)
        {
            WriteHeader(relativePath);
            return true;
        }

        // 3. Processa il file (potenzialmente parzialmente)
        var truncationInfo = ProcessFileWithTruncationInfo(filePath, relativePath);

        // 4. Riga vuota separatrice + flush
        WriteLine();
        Flush();

        // 5. Determina se continuare con i file successivi
        bool shouldContinue = DetermineContinuation(truncationInfo);

        return shouldContinue;
    }

    #region Helper privati

    private void WriteHeader(string relativePath)
        => WriteLine(_listOnlyFileNames ? $"### {relativePath} ###" : $"### Contenuto di {relativePath} ###");

    private void LogSkipDuplicate(string filePath)
        => _logger.WriteLog($"Skipped duplicate: {filePath}", LogLevel.DEBUG);

    /// <summary>
    /// Processa il file tracciando le informazioni di troncamento.
    /// </summary>
    private FileTruncationInfo ProcessFileWithTruncationInfo(string filePath, string relativePath)
    {
        filePath = FileHelper.NormalizeLongPath(filePath);
        var info = new FileTruncationInfo();

        // Ottieni info sul file
        try
        {
            var fileInfo = new FileInfo(filePath);
            info.TotalBytes = fileInfo.Length;
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Impossibile ottenere info file per {filePath}: {ex.Message}", LogLevel.WARNING);
            info.TotalBytes = -1;
        }

        // Prima conta le righe totali (se necessario per il report)
        if (_tokenLimitStrategy == TokenLimitStrategy.IncludePartial)
        {
            try
            {
                info.TotalLines = File.ReadAllLines(filePath).Length;
            }
            catch
            {
                info.TotalLines = -1;
            }
        }

        // Scrivi l'header solo ora (dopo aver raccolto le info)
        WriteHeader(relativePath);

        // Processa le righe
        int lineIdx = 0;
        long bytesProcessed = 0;
        bool tokenBudgetExhaustedForFile = false;

        foreach (var line in File.ReadLines(filePath))
        {
            // a) limite righe per file (opz.)
            if (ExceededMaxLines(lineIdx))
            {
                info.ProcessedLines = lineIdx;
                info.ProcessedBytes = bytesProcessed;
                WriteTruncationFooter(info, "Raggiunto limite righe per file");
                break;
            }

            // b) budget token (opz.)
            if (_tokenBudgetCallback != null)
            {
                int tokens = CountTokens(line);
                bool allowed = _tokenBudgetCallback(tokens);

                if (!allowed)
                {
                    tokenBudgetExhaustedForFile = true;
                    _globalTokenBudgetExhausted = true;

                    if (_tokenLimitStrategy == TokenLimitStrategy.IncludePartial)
                    {
                        // Includi parzialmente: scrivi il footer e continua con i prossimi file
                        info.ProcessedLines = lineIdx;
                        info.ProcessedBytes = bytesProcessed;
                        WriteTruncationFooter(info, "Raggiunto limite token");
                        break;
                    }
                    else
                    {
                        // Escludi completamente: non scrivere nulla di più
                        _logger.WriteLog("Budget token esaurito: interrompo il merge.", LogLevel.INFO);
                        return info;
                    }
                }
            }

            // Scrivi la riga
            WriteLine(line);
            lineIdx++;
            bytesProcessed += Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;
        }

        // Aggiorna info finali
        info.ProcessedLines = lineIdx;
        info.ProcessedBytes = tokenBudgetExhaustedForFile ? bytesProcessed : info.TotalBytes;

        // Se il file è stato processato completamente ma con limite righe, segnalalo
        if (_maxLinesPerFile > 0 && lineIdx >= _maxLinesPerFile && !tokenBudgetExhaustedForFile)
        {
            WriteTruncationFooter(info, "Limite righe per file");
        }

        return info;
    }

    /// <summary>
    /// Scrive il footer di troncamento se il file è stato troncato.
    /// </summary>
    private void WriteTruncationFooter(FileTruncationInfo info, string reason)
    {
        if (!info.WasTruncated && info.ProcessedLines >= info.TotalLines)
            return;

        var footer = new StringBuilder();
        footer.Append($"### FILE TRONCATO ({reason}): ");

        // Aggiungi info su byte se disponibili
        if (info.TotalBytes > 0)
        {
            long missingBytes = info.TotalBytes - info.ProcessedBytes;
            footer.Append($"Processati {FormatBytes(info.ProcessedBytes)}/{FormatBytes(info.TotalBytes)} ");
            footer.Append($"({info.PercentageProcessed:F1}%), ");
            footer.Append($"mancano {FormatBytes(missingBytes)} ");
        }

        // Aggiungi info su righe se disponibili
        if (info.TotalLines > 0)
        {
            int missingLines = info.TotalLines - info.ProcessedLines;
            footer.Append($"- Righe: {info.ProcessedLines}/{info.TotalLines} ");
            footer.Append($"(mancano {missingLines} righe)");
        }
        else if (info.ProcessedLines > 0)
        {
            footer.Append($"- Processate {info.ProcessedLines} righe");
        }

        footer.Append(" ###");

        WriteLine(footer.ToString());
    }

    /// <summary>
    /// Formatta i byte in formato leggibile (KB, MB, etc).
    /// </summary>
    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:F1} {sizes[order]}";
    }

    /// <summary>
    /// Determina se continuare con i file successivi basandosi sul risultato del processing.
    /// </summary>
    private bool DetermineContinuation(FileTruncationInfo info)
    {
        if (_tokenLimitStrategy == TokenLimitStrategy.ExcludeComplete && _globalTokenBudgetExhausted)
        {
            return false; // Stop processing altri file
        }

        // Con IncludePartial continuiamo sempre (anche se il budget è esaurito)
        return true;
    }

    private bool ExceededMaxLines(int lineIdx)
        => _maxLinesPerFile > 0 && lineIdx >= _maxLinesPerFile;

    /// <summary>Flush esplicito: evita file troncati se il chiamante non chiama Dispose().</summary>
    private void Flush() => _writer?.Flush();

    #endregion

    /// <summary>Conta i "token" in maniera approssimativa come numero di parole.</summary>
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