using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CombineFiles.Core.Helpers;

namespace CombineFiles.Core.Services;

/// <summary>
/// Servizio per analisi e statistiche avanzate sui file processati.
/// </summary>
public class AnalyticsService
{
    /// <summary>
    /// Statistiche su un insieme di file.
    /// </summary>
    public class FileStatistics
    {
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public int TotalLines { get; set; }
        public int TotalTokens { get; set; }
        public Dictionary<string, int> FilesByExtension { get; set; } = new();
        public Dictionary<string, long> SizeByExtension { get; set; } = new();
        public Dictionary<string, int> LanguageDistribution { get; set; } = new();
        public DateTime? OldestFile { get; set; }
        public DateTime? NewestFile { get; set; }
        public double AverageFileSize { get; set; }
        public double AverageLinesPerFile { get; set; }
    }

    /// <summary>
    /// Analizza una lista di file e genera statistiche.
    /// </summary>
    public FileStatistics AnalyzeFiles(List<string> filePaths)
    {
        var stats = new FileStatistics();
        if (filePaths == null || filePaths.Count == 0)
            return stats;

        stats.TotalFiles = filePaths.Count;
        var fileInfos = new List<(string path, FileInfo info, int lines, int tokens)>();

        foreach (var path in filePaths)
        {
            try
            {
                var normalized = FileHelper.NormalizeLongPath(path);
                var fi = new FileInfo(normalized);
                int lines = CountLines(normalized);
                int tokens = EstimateTokens(normalized, fi);

                fileInfos.Add((normalized, fi, lines, tokens));

                stats.TotalSize += fi.Length;
                stats.TotalLines += lines;
                stats.TotalTokens += tokens;

                // Aggrega per estensione
                string ext = fi.Extension.ToLower();
                if (string.IsNullOrEmpty(ext)) ext = "(no extension)";

                int currentCount = stats.FilesByExtension.ContainsKey(ext) ? stats.FilesByExtension[ext] : 0;
                stats.FilesByExtension[ext] = currentCount + 1;
                
                long currentSize = stats.SizeByExtension.ContainsKey(ext) ? stats.SizeByExtension[ext] : 0;
                stats.SizeByExtension[ext] = currentSize + fi.Length;

                // Rileva linguaggio dall'estensione
                string language = DetectLanguage(ext);
                int currentLangCount = stats.LanguageDistribution.ContainsKey(language) ? stats.LanguageDistribution[language] : 0;
                stats.LanguageDistribution[language] = currentLangCount + 1;

                // Data più vecchia/nuova
                if (!stats.OldestFile.HasValue || fi.LastWriteTime < stats.OldestFile.Value)
                    stats.OldestFile = fi.LastWriteTime;
                if (!stats.NewestFile.HasValue || fi.LastWriteTime > stats.NewestFile.Value)
                    stats.NewestFile = fi.LastWriteTime;
            }
            catch
            {
                // Ignora file con errori
            }
        }

        if (stats.TotalFiles > 0)
        {
            stats.AverageFileSize = (double)stats.TotalSize / stats.TotalFiles;
            stats.AverageLinesPerFile = (double)stats.TotalLines / stats.TotalFiles;
        }

        return stats;
    }

    /// <summary>
    /// Genera un report HTML con statistiche e grafici.
    /// </summary>
    public string GenerateHtmlReport(FileStatistics stats, string title = "CombineFiles Analytics Report")
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head>");
        sb.AppendLine($"<title>{title}</title>");
        sb.AppendLine("<meta charset='utf-8'>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine("h1, h2 { color: #333; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; margin: 20px 0; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        sb.AppendLine("th { background-color: #4CAF50; color: white; }");
        sb.AppendLine("tr:nth-child(even) { background-color: #f2f2f2; }");
        sb.AppendLine(".stat-box { display: inline-block; margin: 10px; padding: 15px; border: 2px solid #4CAF50; border-radius: 5px; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine($"<h1>{title}</h1>");
        sb.AppendLine($"<p>Generato il: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");

        // Statistiche generali
        sb.AppendLine("<h2>Statistiche Generali</h2>");
        sb.AppendLine("<div class='stat-box'>");
        sb.AppendLine($"<strong>File Totali:</strong> {stats.TotalFiles}<br>");
        sb.AppendLine($"<strong>Dimensione Totale:</strong> {FormatBytes(stats.TotalSize)}<br>");
        sb.AppendLine($"<strong>Righe Totali:</strong> {stats.TotalLines:N0}<br>");
        sb.AppendLine($"<strong>Token Totali:</strong> {stats.TotalTokens:N0}<br>");
        sb.AppendLine($"<strong>Dimensione Media:</strong> {FormatBytes((long)stats.AverageFileSize)}<br>");
        sb.AppendLine($"<strong>Righe Medie per File:</strong> {stats.AverageLinesPerFile:F1}");
        sb.AppendLine("</div>");

        if (stats.OldestFile.HasValue)
            sb.AppendLine($"<p><strong>File più vecchio:</strong> {stats.OldestFile.Value:yyyy-MM-dd HH:mm:ss}</p>");
        if (stats.NewestFile.HasValue)
            sb.AppendLine($"<p><strong>File più recente:</strong> {stats.NewestFile.Value:yyyy-MM-dd HH:mm:ss}</p>");

        // Distribuzione per estensione
        if (stats.FilesByExtension.Count > 0)
        {
            sb.AppendLine("<h2>Distribuzione per Estensione</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Estensione</th><th>Numero File</th><th>Dimensione Totale</th><th>% File</th></tr>");
            foreach (var kvp in stats.FilesByExtension.OrderByDescending(x => x.Value))
            {
                double percentage = (double)kvp.Value / stats.TotalFiles * 100;
                long size = stats.SizeByExtension.ContainsKey(kvp.Key) ? stats.SizeByExtension[kvp.Key] : 0;
                sb.AppendLine($"<tr><td>{kvp.Key}</td><td>{kvp.Value}</td><td>{FormatBytes(size)}</td><td>{percentage:F1}%</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        // Distribuzione per linguaggio
        if (stats.LanguageDistribution.Count > 0)
        {
            sb.AppendLine("<h2>Distribuzione per Linguaggio</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Linguaggio</th><th>Numero File</th><th>%</th></tr>");
            foreach (var kvp in stats.LanguageDistribution.OrderByDescending(x => x.Value))
            {
                double percentage = (double)kvp.Value / stats.TotalFiles * 100;
                sb.AppendLine($"<tr><td>{kvp.Key}</td><td>{kvp.Value}</td><td>{percentage:F1}%</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    /// <summary>
    /// Genera un report in formato testo semplice.
    /// </summary>
    public string GenerateTextReport(FileStatistics stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== CombineFiles Analytics Report ===");
        sb.AppendLine($"Generato il: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("Statistiche Generali:");
        sb.AppendLine($"  File Totali: {stats.TotalFiles}");
        sb.AppendLine($"  Dimensione Totale: {FormatBytes(stats.TotalSize)}");
        sb.AppendLine($"  Righe Totali: {stats.TotalLines:N0}");
        sb.AppendLine($"  Token Totali: {stats.TotalTokens:N0}");
        sb.AppendLine($"  Dimensione Media: {FormatBytes((long)stats.AverageFileSize)}");
        sb.AppendLine($"  Righe Medie per File: {stats.AverageLinesPerFile:F1}");
        if (stats.OldestFile.HasValue)
            sb.AppendLine($"  File più vecchio: {stats.OldestFile.Value:yyyy-MM-dd HH:mm:ss}");
        if (stats.NewestFile.HasValue)
            sb.AppendLine($"  File più recente: {stats.NewestFile.Value:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        if (stats.FilesByExtension.Count > 0)
        {
            sb.AppendLine("Distribuzione per Estensione:");
            foreach (var kvp in stats.FilesByExtension.OrderByDescending(x => x.Value))
            {
                double percentage = (double)kvp.Value / stats.TotalFiles * 100;
                long size = stats.SizeByExtension.ContainsKey(kvp.Key) ? stats.SizeByExtension[kvp.Key] : 0;
                sb.AppendLine($"  {kvp.Key}: {kvp.Value} file ({percentage:F1}%), {FormatBytes(size)}");
            }
            sb.AppendLine();
        }

        if (stats.LanguageDistribution.Count > 0)
        {
            sb.AppendLine("Distribuzione per Linguaggio:");
            foreach (var kvp in stats.LanguageDistribution.OrderByDescending(x => x.Value))
            {
                double percentage = (double)kvp.Value / stats.TotalFiles * 100;
                sb.AppendLine($"  {kvp.Key}: {kvp.Value} file ({percentage:F1}%)");
            }
        }

        return sb.ToString();
    }

    private int CountLines(string filePath)
    {
        try
        {
            return File.ReadLines(filePath).Count();
        }
        catch
        {
            return 0;
        }
    }

    private int EstimateTokens(string filePath, FileInfo fi)
    {
        try
        {
            // Stima basata sulla dimensione (simile a FileCollector)
            string extension = fi.Extension.ToLower();
            double tokensPerByte = extension switch
            {
                ".cs" or ".java" or ".cpp" or ".c" or ".h" => 0.20,
                ".py" or ".js" or ".ts" => 0.22,
                ".xml" or ".html" or ".xaml" => 0.15,
                ".json" or ".yaml" or ".yml" => 0.18,
                ".txt" or ".md" or ".rst" => 0.25,
                ".sql" => 0.20,
                ".css" or ".scss" or ".less" => 0.18,
                ".log" => 0.25,
                ".config" or ".ini" or ".properties" => 0.20,
                _ => 0.25
            };

            return Math.Max(1, (int)Math.Ceiling(fi.Length * tokensPerByte));
        }
        catch
        {
            return 1;
        }
    }

    private string DetectLanguage(string extension)
    {
        return extension switch
        {
            ".cs" => "C#",
            ".vb" => "VB.NET",
            ".java" => "Java",
            ".py" => "Python",
            ".js" or ".jsx" => "JavaScript",
            ".ts" or ".tsx" => "TypeScript",
            ".cpp" or ".cxx" or ".cc" or ".c" or ".h" or ".hpp" => "C/C++",
            ".html" or ".htm" or ".xhtml" => "HTML",
            ".css" or ".scss" or ".less" => "CSS",
            ".xml" or ".xaml" => "XML",
            ".json" => "JSON",
            ".sql" => "SQL",
            ".md" or ".markdown" => "Markdown",
            ".txt" => "Text",
            ".log" => "Log",
            _ => "Other"
        };
    }

    private static string FormatBytes(long b)
    {
        string[] u = { "B", "KB", "MB", "GB" };
        double n = b; int i = 0;
        while (n >= 1024 && i < u.Length - 1) { n /= 1024; i++; }
        return $"{n:F1} {u[i]}";
    }
}

