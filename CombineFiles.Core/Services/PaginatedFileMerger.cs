using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.Core.Services;

/// <summary>
/// FileMerger che supporta paginazione dell'output quando si supera MaxTokensPerPage.
/// Genera file multipli: output_001.txt, output_002.txt, ecc.
/// </summary>
public sealed class PaginatedFileMerger : IDisposable
{
    private readonly Logger _logger;
    private readonly bool _outputToConsole;
    private readonly string? _baseOutputFile;
    private readonly bool _listOnlyFileNames;
    private readonly int _maxLinesPerFile;
    private readonly int _maxTokensPerPage;
    private readonly int _tokensPerFile;
    private readonly bool _compactSpacesToTabs;
    private readonly bool _compactForLLM;

    private StreamWriter? _currentWriter;
    private int _currentPage;
    private int _currentPageTokens;
    private readonly SHA256 _sha = SHA256.Create();
    private readonly System.Collections.Generic.HashSet<string> _processedHashes = new(System.StringComparer.OrdinalIgnoreCase);
    private readonly string _baseDir = Directory.GetCurrentDirectory();
    private readonly System.Collections.Generic.List<string> _currentPageLines = new(); // Per compattazione LLM

    public PaginatedFileMerger(
        Logger logger,
        bool outputToConsole,
        string? baseOutputFile,
        bool listOnlyFileNames,
        int maxLinesPerFile,
        int maxTokensPerPage,
        int tokensPerFile = 0,
        bool compactSpacesToTabs = false,
        bool compactForLLM = false)
    {
        _logger = logger;
        _outputToConsole = outputToConsole;
        _baseOutputFile = baseOutputFile;
        _listOnlyFileNames = listOnlyFileNames;
        _maxLinesPerFile = maxLinesPerFile;
        _maxTokensPerPage = maxTokensPerPage;
        _tokensPerFile = tokensPerFile;
        _compactSpacesToTabs = compactSpacesToTabs;
        _compactForLLM = compactForLLM;
        _currentPage = 1;
        _currentPageTokens = 0;

        if (!_outputToConsole && !string.IsNullOrWhiteSpace(_baseOutputFile))
        {
            OpenNewPage();
        }
    }

    public void Dispose()
    {
        _currentWriter?.Dispose();
        _sha.Dispose();
    }

    public bool MergeFile(string filePath, bool avoidDuplicatesByHash = true)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException(nameof(filePath));

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

        ProcessFileWithPagination(filePath, relative);
        return true;
    }

    private void ProcessFileWithPagination(string fullPath, string relativePath)
    {
        fullPath = FileHelper.NormalizeLongPath(fullPath);
        WriteHeader(relativePath);

        int lines = 0;
        int tokens = 0;

        // Rileva encoding del file
        Encoding fileEncoding = EncodingDetector.DetectEncoding(fullPath);

        foreach (var line in File.ReadLines(fullPath, fileEncoding))
        {
            if (_maxLinesPerFile > 0 && lines >= _maxLinesPerFile)
                break;

            int lineTokens = CountTokens(line);

            // Controlla se dobbiamo creare una nuova pagina
            if (_maxTokensPerPage > 0 && _currentPageTokens + lineTokens > _maxTokensPerPage && _currentPageTokens > 0)
            {
                CloseCurrentPage();
                OpenNewPage();
                WriteHeader(relativePath); // Riscrivi header nella nuova pagina
            }

            // Applica compattazione spazi se richiesta
            string processedLine = line;
            if (_compactSpacesToTabs)
                processedLine = Helpers.TextCompactor.CompactSpacesToTabs(processedLine);

            WriteLine(processedLine);
            lines++;
            tokens += lineTokens;
            _currentPageTokens += lineTokens;

            // Controlla anche limite token per file
            if (_tokensPerFile > 0 && tokens > _tokensPerFile)
                break;
        }

        WriteLine();
        Flush();
    }

    private void OpenNewPage()
    {
        if (_outputToConsole || string.IsNullOrWhiteSpace(_baseOutputFile))
            return;

        _currentWriter?.Dispose();
        _currentPageLines.Clear();

        string pageFile = GetPageFileName(_currentPage);
        _currentWriter = new StreamWriter(pageFile, false, new UTF8Encoding(false));
        _currentPageTokens = 0;

        _logger.WriteLog($"Aperta pagina {_currentPage}: {pageFile}", LogLevel.INFO);
    }

    private void CloseCurrentPage()
    {
        if (_currentWriter != null)
        {
            // Se compattazione LLM è attiva, applica a tutta la pagina accumulata
            if (_compactForLLM && _currentPageLines.Count > 0)
            {
                string fullPage = string.Join(Environment.NewLine, _currentPageLines);
                fullPage = Helpers.TextCompactor.CompactForLLM(fullPage);
                _currentWriter.Write(fullPage);
                _currentWriter.WriteLine();
                _currentWriter.WriteLine($"### Fine pagina {_currentPage} ###");
                _currentPageLines.Clear();
            }
            else
            {
                // Se non abbiamo accumulato, scriviamo normalmente
                if (!_compactForLLM)
                {
                    WriteLine();
                    WriteLine($"### Fine pagina {_currentPage} ###");
                }
            }
            
            _currentWriter.Flush();
            _currentWriter.Dispose();
            _currentWriter = null;
        }
        _currentPage++;
    }

    private string GetPageFileName(int pageNumber)
    {
        string dir = Path.GetDirectoryName(_baseOutputFile) ?? Directory.GetCurrentDirectory();
        string fileName = Path.GetFileNameWithoutExtension(_baseOutputFile);
        string extension = Path.GetExtension(_baseOutputFile);
        return Path.Combine(dir, $"{fileName}_{pageNumber:D3}{extension}");
    }

    private void WriteHeader(string rel)
    {
        WriteLine($"### Contenuto di {rel} ###");
    }

    private void WriteLine(string s = "")
    {
        // Se compattazione LLM è attiva e stiamo scrivendo su file, accumula le righe
        if (_compactForLLM && !_outputToConsole && _currentWriter != null)
        {
            _currentPageLines.Add(s);
        }
        else
        {
            // Scrittura diretta (console o file senza compattazione LLM)
            if (_outputToConsole)
                Console.WriteLine(s);
            else
                _currentWriter?.WriteLine(s);
        }
    }

    private void Flush()
    {
        _currentWriter?.Flush();
    }

    private static int CountTokens(string line) =>
        string.IsNullOrWhiteSpace(line)
            ? 0
            : line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;

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

    /// <summary>
    /// Chiude la pagina corrente e genera un file di indice con l'elenco delle pagine create.
    /// </summary>
    public void FinalizePages()
    {
        // Se c'è ancora contenuto accumulato, scrivilo prima di chiudere
        if (_compactForLLM && _currentPageLines.Count > 0 && _currentWriter != null)
        {
            string fullPage = string.Join(Environment.NewLine, _currentPageLines);
            fullPage = Helpers.TextCompactor.CompactForLLM(fullPage);
            _currentWriter.Write(fullPage);
            _currentWriter.Flush();
            _currentPageLines.Clear();
        }

        if (!_outputToConsole && !string.IsNullOrWhiteSpace(_baseOutputFile) && _currentPage > 1)
        {
            CloseCurrentPage();

            // Genera file indice
            string indexFile = Path.Combine(
                Path.GetDirectoryName(_baseOutputFile) ?? Directory.GetCurrentDirectory(),
                Path.GetFileNameWithoutExtension(_baseOutputFile) + "_index.txt");

            using var indexWriter = new StreamWriter(indexFile, false, new UTF8Encoding(false));
            indexWriter.WriteLine("### Indice delle pagine generate ###");
            indexWriter.WriteLine();

            for (int i = 1; i < _currentPage; i++)
            {
                string pageFile = GetPageFileName(i);
                if (File.Exists(pageFile))
                {
                    var fi = new FileInfo(pageFile);
                    indexWriter.WriteLine($"{i:D3}: {Path.GetFileName(pageFile)} ({FormatBytes(fi.Length)})");
                }
            }

            _logger.WriteLog($"Generato file indice: {indexFile}", LogLevel.INFO);
        }
        else
        {
            CloseCurrentPage();
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

