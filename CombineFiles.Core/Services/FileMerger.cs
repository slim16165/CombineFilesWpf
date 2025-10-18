using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.Core.Services;

/// <summary>Strategie quando si raggiunge il limite di token.</summary>
public enum TokenLimitStrategy
{
    ExcludeCompletely, // interrompe il merge appena un file sfora
    IncludePartial,    // tronca il file che sfora ma continua con i successivi
    PaginateOutput     // (non usato qui)
}

/// <summary>Info sul troncamento di un file.</summary>
public sealed class FileTruncationInfo
{
    public long TotalBytes { get; set; } = -1;
    public long ProcessedBytes { get; set; }
    public int ProcessedLines { get; set; }

    public bool WasTruncated => TotalBytes >= 0 && ProcessedBytes < TotalBytes;
    public double PercentageProcessed =>
        TotalBytes > 0 ? ProcessedBytes * 100.0 / TotalBytes : 0;
}

/// <summary>
/// Unisce (o elenca) i file di testo applicando un limite **costante** di token per file.
/// Il limite viene passato tramite il parametro già esistente <paramref name="maxTokensPerPage"/>.
/// </summary>
public sealed class FileMerger : IDisposable
{
    private readonly Logger _logger;
    private readonly bool _outputToConsole;
    private readonly StreamWriter? _writer;
    private readonly bool _listOnlyFileNames;
    private readonly int _maxLinesPerFile;
    private readonly int _tokensPerFile;          // <-- QUI
    private readonly TokenLimitStrategy _tokenLimitStrategy;

    private readonly SHA256 _sha = SHA256.Create();
    private readonly HashSet<string> _processedHashes = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _baseDir = Directory.GetCurrentDirectory();

    private bool _budgetViolated; // serve solo con ExcludeCompletely

    /* ---------- CTOR ---------- */
    public FileMerger(
        Logger logger,
        bool outputToConsole,
        string? outputFile,
        bool listOnlyFileNames,
        int maxLinesPerFile,
        TokenLimitStrategy tokenLimitStrategy,
        int tokensPerFile
    )
    {
        _logger = logger;
        _outputToConsole = outputToConsole;
        _listOnlyFileNames = listOnlyFileNames;
        _maxLinesPerFile = maxLinesPerFile;
        _tokensPerFile = tokensPerFile;  // 0 = nessun limite
        _tokenLimitStrategy = tokenLimitStrategy;

        if (!_outputToConsole)
        {
            if (string.IsNullOrWhiteSpace(outputFile))
                throw new ArgumentException("outputFile cannot be null/empty.", nameof(outputFile));

            _writer = new StreamWriter(outputFile, false, new UTF8Encoding(false));
        }
    }

    public void Dispose()
    {
        _writer?.Dispose();
        _sha.Dispose();
    }

    /* ---------- API PRINCIPALE ---------- */
    public bool MergeFile(string filePath, bool avoidDuplicatesByHash = true)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException(nameof(filePath));

        // stop immediato se la strategia è ExcludeCompletely e abbiamo già violato il budget
        if (_budgetViolated && _tokenLimitStrategy == TokenLimitStrategy.ExcludeCompletely)
            return false;

        // deduplicazione opzionale
        if (avoidDuplicatesByHash && IsDuplicate(filePath))
        {
            _logger.WriteLog($"Skipped duplicate: {filePath}", LogLevel.DEBUG);
            return true;
        }

        var relative = FileHelper.GetRelativePath(_baseDir, filePath);

        if (_listOnlyFileNames)
        {
            WriteHeader(relative);
            return true;
        }

        var truncInfo = ProcessFile(filePath, relative);

        WriteLine(); _writer?.Flush();

        return !(_budgetViolated && _tokenLimitStrategy == TokenLimitStrategy.ExcludeCompletely);
    }

    // Overload: merge con limite token per questo file
    public bool MergeFile(string filePath, int maxTokensForThisFile, bool avoidDuplicatesByHash = true)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException(nameof(filePath));

        if (_budgetViolated && _tokenLimitStrategy == TokenLimitStrategy.ExcludeCompletely)
            return false;

        if (avoidDuplicatesByHash && IsDuplicate(filePath))
        {
            _logger.WriteLog($"Skipped duplicate: {filePath}", LogLevel.DEBUG);
            return true;
        }

        var relative = FileHelper.GetRelativePath(_baseDir, filePath);

        if (_listOnlyFileNames)
        {
            WriteHeader(relative);
            return true;
        }

        var truncInfo = ProcessFile(filePath, relative, maxTokensForThisFile);

        WriteLine(); _writer?.Flush();

        return !(_budgetViolated && _tokenLimitStrategy == TokenLimitStrategy.ExcludeCompletely);
    }

    /* ---------- CORE ---------- */
    private FileTruncationInfo ProcessFile(string fullPath, string relativePath)
    {
        fullPath = FileHelper.NormalizeLongPath(fullPath);
        long fileSize = TryGetFileSize(fullPath);

        WriteHeader(relativePath);

        long bytes = 0;
        int lines = 0;
        int tokens = 0;
        bool truncated = false;

        foreach (var line in File.ReadLines(fullPath))
        {
            if (_maxLinesPerFile > 0 && lines >= _maxLinesPerFile)
            {
                truncated = true;
                break;
            }

            int lineTokens = CountTokens(line);
            if (_tokensPerFile > 0 && tokens + lineTokens > _tokensPerFile)
            {
                truncated = true;
                break;
            }

            WriteLine(line);
            lines++;
            tokens += lineTokens;
            bytes += Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;
        }

        if (truncated)
        {
            _budgetViolated = true;
            var info = new FileTruncationInfo
            {
                TotalBytes = fileSize,
                ProcessedBytes = bytes,
                ProcessedLines = lines
            };
            WriteTruncationFooter(info);
            return info;
        }

        return new FileTruncationInfo
        {
            TotalBytes = fileSize,
            ProcessedBytes = bytes,
            ProcessedLines = lines
        };
    }

    // Overload di ProcessFile con limite token per-file
    private FileTruncationInfo ProcessFile(string fullPath, string relativePath, int maxTokensForThisFile)
    {
        fullPath = FileHelper.NormalizeLongPath(fullPath);
        long fileSize = TryGetFileSize(fullPath);

        WriteHeader(relativePath);

        long bytes = 0;
        int lines = 0;
        int tokens = 0;
        bool truncated = false;
        int tokenLimit = maxTokensForThisFile > 0 ? maxTokensForThisFile : _tokensPerFile;

        foreach (var line in File.ReadLines(fullPath))
        {
            if (_maxLinesPerFile > 0 && lines >= _maxLinesPerFile)
            {
                truncated = true;
                break;
            }

            int lineTokens = CountTokens(line);
            if (tokenLimit > 0 && tokens + lineTokens > tokenLimit)
            {
                truncated = true;
                break;
            }

            WriteLine(line);
            lines++;
            tokens += lineTokens;
            bytes += Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;
        }

        if (truncated)
        {
            _budgetViolated = true;
            var info = new FileTruncationInfo
            {
                TotalBytes = fileSize,
                ProcessedBytes = bytes,
                ProcessedLines = lines
            };
            WriteTruncationFooter(info);
            return info;
        }

        return new FileTruncationInfo
        {
            TotalBytes = fileSize,
            ProcessedBytes = bytes,
            ProcessedLines = lines
        };
    }

    /* ---------- IO helper ---------- */
    private void WriteHeader(string rel)
        => WriteLine($"### Contenuto di {rel} ###");

    private void WriteTruncationFooter(FileTruncationInfo info)
    {
        var sb = new StringBuilder("### FILE TRONCATO: ");
        if (info.TotalBytes > 0)
            sb.Append($"{FormatBytes(info.ProcessedBytes)}/{FormatBytes(info.TotalBytes)}  ");
        sb.Append($"Righe {info.ProcessedLines}");
        sb.Append(" ###");
        WriteLine(sb.ToString());
    }

    private void WriteLine(string s = "")
    {
        if (_outputToConsole) Console.WriteLine(s);
        else _writer!.WriteLine(s);
    }

    /* ---------- UTIL ---------- */
    private static int CountTokens(string line) =>
        string.IsNullOrWhiteSpace(line)
            ? 0
            : line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;

    private static long TryGetFileSize(string p)
    {
        try { return new FileInfo(p).Length; } catch { return -1; }
    }

    private bool IsDuplicate(string filePath)
    {
        try
        {
            using var fs = File.OpenRead(filePath);
            var hash = Convert.ToBase64String(_sha.ComputeHash(fs));
            return !_processedHashes.Add(hash);
        }
        catch
        {
            return false;
        }
    }

    private static string FormatBytes(long b)
    {
        string[] u = { "B", "KB", "MB", "GB" };
        double n = b; int i = 0;
        while (n >= 1024 && i < u.Length - 1) { n /= 1024; i++; }
        return $"{n:F1} {u[i]}";
    }
}