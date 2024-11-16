using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CombineFiles.Core.Helpers;
using CsvHelper;

namespace CombineFiles.Core;

public static class FileProcessor
{
    /// <summary>
    /// Metodo per ottenere la lista di file validi, su cui operare.
    /// Puoi estenderlo con filtri, regex, estensioni, etc.
    /// </summary>
    public static IEnumerable<string> GetFilesToProcess(string[] paths, FileSearchConfig? config = null)
    {
        config ??= new FileSearchConfig();

        var validFiles = new List<string>();
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                var files = Directory.EnumerateFiles(path, "*.*",
                    config.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                validFiles.AddRange(files.Where(f => FileFilterHelper.ShouldIncludeFile(
                    f, config.ExcludeHidden, config.ExcludePaths, config.ExcludeExtensions, config.IncludeExtensions)));
            }
            else if (File.Exists(path))
            {
                if (FileFilterHelper.ShouldIncludeFile(path, config.ExcludeHidden, config.ExcludePaths,
                        config.ExcludeExtensions, config.IncludeExtensions))
                {
                    validFiles.Add(path);
                }
            }
        }
        return validFiles;
    }

    /// <summary>
    /// Esempio di metodo che unisce i contenuti di una lista di file.
    /// </summary>
    public static string CombineContents(IEnumerable<string> files)
    {
        var sb = new StringBuilder();
        foreach (var file in files)
        {
            sb.AppendLine($"### {Path.GetFileName(file)} ###");
            try
            {
                string content = File.ReadAllText(file);
                string extension = Path.GetExtension(file).ToLower();
                switch (extension)
                {
                    case ".csv":
                        sb.AppendLine(ConvertCsvToTable(content));
                        break;
                    case ".json":
                        sb.AppendLine(PrettyPrintJson(content));
                        break;
                    default:
                        sb.AppendLine(content);
                        break;
                }
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[ERRORE: impossibile leggere {file} - {ex.Message}]");
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Esempio di metodo che formatta un contenuto CSV come tabella (semplificato).
    /// </summary>
    public static string ConvertCsvToTable(string csvContent)
    {
        using var reader = new StringReader(csvContent);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<dynamic>().ToList();
        if (!records.Any()) return string.Empty;

        var headers = csv.HeaderRecord;
        var table = new StringBuilder();
        table.AppendLine(string.Join(" | ", headers!));
        table.AppendLine(new string('-', headers.Length * 4));
        foreach (var record in records)
        {
            table.AppendLine(string.Join(" | ", ((IDictionary<string, object>)record).Values));
        }
        return table.ToString();
    }

    public static string PrettyPrintJson(string content)
    {
        var doc = JsonDocument.Parse(content);
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }
}