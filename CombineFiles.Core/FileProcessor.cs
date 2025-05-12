using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CombineFiles.Core.Configuration;
using CombineFiles.Core.Handlers;
using CombineFiles.Core.Helpers;

namespace CombineFiles.Core;

[Obsolete("Usato solo dalla versione standalone")]
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

    public static string CombineContents(IEnumerable<string> files)
    {
        // Utilizzo di un comparatore case-insensitive per gestire correttamente le estensioni
        var handlers = new Dictionary<string, IFileContentHandler>(StringComparer.OrdinalIgnoreCase)
        {
            { ".csv", new CsvContentHandler() },
            { ".json", new JsonContentHandler() }
        };

        var sb = new StringBuilder();
        foreach (var file in files)
        {
            sb.AppendLine($"### {Path.GetFileName(file)} ###");
            try
            {
                string content = File.ReadAllText(file);
                string extension = Path.GetExtension(file);

                if (!handlers.TryGetValue(extension, out var handler))
                {
                    handler = new DefaultContentHandler();
                }
                sb.AppendLine(handler.Handle(content));
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[ERROR: unable to read {file} - {ex.Message}]");
            }
        }
        return sb.ToString();
    }
}