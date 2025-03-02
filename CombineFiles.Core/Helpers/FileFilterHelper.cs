using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CombineFiles.Core.Configuration;

namespace CombineFiles.Core.Helpers;

public static class FileFilterHelper
{

    /// <summary>
    /// Verifica se il file deve essere incluso in base alle opzioni di filtro.
    /// </summary>
    /// <param name="filePath">Il percorso completo del file.</param>
    /// <param name="excludeHidden">Se escludere i file nascosti.</param>
    /// <param name="excludePaths">Percorsi (separati da virgola o punto e virgola) da escludere.</param>
    /// <param name="excludeExtensions">Estensioni (separati da virgola o punto e virgola) da escludere.</param>
    /// <param name="includeExtensions">Se specificato, includi solo questi file (separati da virgola o punto e virgola).</param>
    /// <returns>true se il file deve essere incluso, altrimenti false.</returns>
    public static bool ShouldIncludeFile(string filePath, bool excludeHidden, string excludePaths, string excludeExtensions, string includeExtensions)
    {
        // Escludi file nascosti
        if (excludeHidden && (new FileInfo(filePath).Attributes.HasFlag(FileAttributes.Hidden)))
        {
            return false;
        }

        // Escludi percorsi specifici
        if (!string.IsNullOrWhiteSpace(excludePaths))
        {
            var paths = excludePaths.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var path in paths)
            {
                if (filePath.IndexOf(path, StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
            }
        }

        string extension = Path.GetExtension(filePath).ToLower();

        // Escludi estensioni specifiche
        if (!string.IsNullOrWhiteSpace(excludeExtensions))
        {
            var extList = excludeExtensions.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);
            if (Array.Exists(extList, e => e.Equals(extension, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        // Includi solo estensioni specifiche se definite
        if (!string.IsNullOrWhiteSpace(includeExtensions))
        {
            var includeList = includeExtensions.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);
            if (!Array.Exists(includeList, e => e.Equals(extension, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Verifica se il file deve essere incluso in base alle opzioni di filtro.
    /// </summary>
    /// <param name="filePath">Il percorso completo del file.</param>
    /// <param name="excludeHidden">Se escludere i file nascosti.</param>
    /// <param name="excludePaths">Percorsi (separati da virgola o punto e virgola) da escludere.</param>
    /// <param name="excludeExtensions">Estensioni (separati da virgola o punto e virgola) da escludere.</param>
    /// <param name="includeExtensions">Se specificato, includi solo questi file (separati da virgola o punto e virgola).</param>
    /// <returns>true se il file deve essere incluso, altrimenti false.</returns>
    public static bool ShouldIncludeFile(string filePath, FileSearchConfig config)
    {
        var fileInfo = new FileInfo(filePath);
        if (config.ExcludeHidden && fileInfo.Attributes.HasFlag(FileAttributes.Hidden)) return false;

        var excludePaths = config.ExcludePaths?.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        if (excludePaths.Any(p => filePath.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0)) return false;

        string extension = Path.GetExtension(filePath).ToLower();
        var excludeExts = config.ExcludeExtensions?.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim().ToLower()).ToHashSet() ?? [];
        if (excludeExts.Contains(extension)) return false;

        var includeExts = config.IncludeExtensions?.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim().ToLower()).ToHashSet() ?? [];
        if (includeExts.Any() && !includeExts.Contains(extension)) return false;

        return true;
    }
}
