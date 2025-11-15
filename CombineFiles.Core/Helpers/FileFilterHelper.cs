using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CombineFiles.Core.Configuration;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.Core.Helpers
{
    public static class FileFilterHelper
    {
        /// <summary>
        /// Verifica se un file deve essere incluso in base a:
        /// - File nascosti (opzionale)
        /// - Percorsi da escludere
        /// - Estensioni da includere/escludere
        /// - Nome contenente "auto-generated"
        /// </summary>
        /// <param name="filePath">Il percorso completo del file.</param>
        /// <param name="excludeHidden">Se escludere file nascosti.</param>
        /// <param name="excludePaths">Percorsi da escludere (separati da virgola o punto e virgola).</param>
        /// <param name="excludeExtensions">Estensioni da escludere (separati da virgola o punto e virgola).</param>
        /// <param name="includeExtensions">Estensioni da includere (separati da virgola o punto e virgola).</param>
        /// <param name="logger">Facoltativo, se vuoi loggare dettagli in modalità DEBUG.</param>
        public static bool ShouldIncludeFile(
            string filePath,
            bool excludeHidden,
            string excludePaths,
            string excludeExtensions,
            string includeExtensions,
            Logger? logger = null)
        {
            // 1) Escludi file nascosti
            if (excludeHidden)
            {
            try
            {
                var attributes = File.GetAttributes(filePath);
                if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    logger?.WriteLog($"File nascosto escluso: {FileHelper.GetRelativePath(Directory.GetCurrentDirectory(), filePath)}", LogLevel.DEBUG);
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger?.WriteLog($"Errore nel leggere attributi di {FileHelper.GetRelativePath(Directory.GetCurrentDirectory(), filePath)}: {ex.Message}", LogLevel.WARNING);
                return false;
            }
            }

            // 2) Esclusione per percorsi (stringa con più valori, divisi da virgola/punto e virgola)
            if (!string.IsNullOrWhiteSpace(excludePaths))
            {
                var pathsToExclude = excludePaths.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(p => p.Trim());
                foreach (var path in pathsToExclude)
                {
                    if (filePath.IndexOf(path, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        logger?.WriteLog($"File escluso per percorso: {FileHelper.GetRelativePath(Directory.GetCurrentDirectory(), filePath)} contiene \"{path}\"", LogLevel.DEBUG);
                        return false;
                    }
                }
            }

            // 3) Esclusione per estensione
            var fileExt = Path.GetExtension(filePath);
            if (!string.IsNullOrWhiteSpace(excludeExtensions))
            {
                var exts = excludeExtensions.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(e => e.Trim());
                if (exts.Any(e => fileExt.Equals(e, StringComparison.OrdinalIgnoreCase)))
                {
                    logger?.WriteLog($"File escluso per estensione: {FileHelper.GetRelativePath(Directory.GetCurrentDirectory(), filePath)} ha estensione \"{fileExt}\"", LogLevel.DEBUG);
                    return false;
                }
            }

            // 4) Inclusione per estensione (se specificata)
            if (!string.IsNullOrWhiteSpace(includeExtensions))
            {
                var includeExts = includeExtensions.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(e => e.Trim());
                if (!includeExts.Any(e => fileExt.Equals(e, StringComparison.OrdinalIgnoreCase)))
                {
                    logger?.WriteLog($"File escluso perché non è tra le estensioni incluse: {FileHelper.GetRelativePath(Directory.GetCurrentDirectory(), filePath)}", LogLevel.DEBUG);
                    return false;
                }
            }

            // 5) Escludi file con "auto-generated" nel nome
            var fileName = Path.GetFileName(filePath);
            if (Regex.IsMatch(fileName, "auto-generated", RegexOptions.IgnoreCase))
            {
                logger?.WriteLog($"File escluso per nome auto-generated: {FileHelper.GetRelativePath(Directory.GetCurrentDirectory(), filePath)}", LogLevel.DEBUG);
                return false;
            }

            return true;
        }
    }
}
