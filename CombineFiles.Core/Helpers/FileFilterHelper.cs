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
                        logger?.WriteLog($"File nascosto escluso: {GetRelativePath(filePath)}", LogLevel.DEBUG);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    logger?.WriteLog($"Errore nel leggere attributi di {GetRelativePath(filePath)}: {ex.Message}", LogLevel.WARNING);
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
                        logger?.WriteLog($"File escluso per percorso: {GetRelativePath(filePath)} contiene \"{path}\"", LogLevel.DEBUG);
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
                    logger?.WriteLog($"File escluso per estensione: {GetRelativePath(filePath)} ha estensione \"{fileExt}\"", LogLevel.DEBUG);
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
                    logger?.WriteLog($"File escluso perché non è tra le estensioni incluse: {GetRelativePath(filePath)}", LogLevel.DEBUG);
                    return false;
                }
            }

            // 5) Escludi file con “auto-generated” nel nome
            var fileName = Path.GetFileName(filePath);
            if (Regex.IsMatch(fileName, "auto-generated", RegexOptions.IgnoreCase))
            {
                logger?.WriteLog($"File escluso per nome auto-generated: {GetRelativePath(filePath)}", LogLevel.DEBUG);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Filtra i file in base alla data di creazione e/o alla dimensione (MinSize, MaxSize).
        /// </summary>
        public static List<string> FilterByDateAndSize(List<string> files, CombineFilesOptions options, Logger logger)
        {
            var filtered = files.Where(file =>
            {
                FileInfo fi;
                try
                {
                    fi = new FileInfo(file);
                }
                catch (Exception ex)
                {
                    logger.WriteLog($"Errore nel leggere informazioni di {GetRelativePath(file)}: {ex.Message}", LogLevel.WARNING);
                    return false;
                }

                // 1) Filtra per dimensione minima
                if (!string.IsNullOrWhiteSpace(options.MinSize))
                {
                    try
                    {
                        long minBytes = FileHelper.ConvertSizeToBytes(options.MinSize);
                        if (fi.Length < minBytes)
                            return false;
                    }
                    catch (Exception ex)
                    {
                        logger.WriteLog($"Errore nel convertire MinSize per {GetRelativePath(file)}: {ex.Message}", LogLevel.WARNING);
                        return false;
                    }
                }

                // 2) Filtra per dimensione massima
                if (!string.IsNullOrWhiteSpace(options.MaxSize))
                {
                    try
                    {
                        long maxBytes = FileHelper.ConvertSizeToBytes(options.MaxSize);
                        if (fi.Length > maxBytes)
                            return false;
                    }
                    catch (Exception ex)
                    {
                        logger.WriteLog($"Errore nel convertire MaxSize per {GetRelativePath(file)}: {ex.Message}", LogLevel.WARNING);
                        return false;
                    }
                }

                // 3) Filtra per data minima
                if (options.MinDate.HasValue && fi.CreationTime < options.MinDate.Value)
                    return false;

                // 4) Filtra per data massima
                if (options.MaxDate.HasValue && fi.CreationTime > options.MaxDate.Value)
                    return false;

                return true;
            }).ToList();

            return filtered;
        }

        /// <summary>
        /// Controlla se il file contiene nel contenuto la stringa "<auto-generated" o "<autogenerated"
        /// (tipico dei file generati da tool come quelli degli AssemblyAttributes).
        /// </summary>
        public static bool FileContainsAutoGenerated(string filePath, Logger logger)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                if (content.IndexOf("<auto-generated", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    content.IndexOf("<autogenerated", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    logger.WriteLog($"File escluso per contenuto auto-generated: {GetRelativePath(filePath)}", LogLevel.DEBUG);
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.WriteLog($"Errore nella lettura di {GetRelativePath(filePath)}: {ex.Message}", LogLevel.WARNING);
            }
            return false;
        }

        /// <summary>
        /// Converte un percorso assoluto in relativo rispetto alla cartella corrente.
        /// Se la conversione fallisce, restituisce il percorso originale.
        /// </summary>
        private static string GetRelativePath(string fullPath)
        {
            try
            {
                return FileHelper.GetRelativePath(Directory.GetCurrentDirectory(), fullPath);
            }
            catch
            {
                return fullPath;
            }
        }
    }
}
