using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CombineFiles.ConsoleApp.Core;
using CombineFiles.ConsoleApp.Infrastructure;

namespace CombineFiles.ConsoleApp.Helpers
{
    public static class FileCollectionHelper
    {
        /// <summary>
        /// Metodo “unico” che, in base a options.Mode, decide come ottenere la lista di file.
        /// </summary>
        public static List<string> CollectFiles(CombineFilesOptions options, Logger logger, FileCollector collector, string sourcePath)
        {
            List<string> filesToProcess;

            switch (options.Mode?.ToLowerInvariant())
            {
                case "list":
                    filesToProcess = HandleListMode(options, logger);
                    break;

                case "extensions":
                    filesToProcess = HandleExtensionsMode(options, logger, collector);
                    break;

                case "regex":
                    filesToProcess = HandleRegexMode(options, logger, collector);
                    break;

                case "interactiveselection":
                    // Ottieni prima i file per estensione
                    filesToProcess = HandleExtensionsMode(options, logger, collector);

                    // Poi avvia la selezione interattiva
                    filesToProcess = collector.StartInteractiveSelection(filesToProcess, sourcePath);
                    break;

                default:
                    // Se non specificato, raccogli tutti i file
                    filesToProcess = collector.GetAllFiles(sourcePath, options.Recurse);
                    break;
            }

            return filesToProcess;
        }

        private static List<string> HandleListMode(CombineFilesOptions options, Logger logger)
        {
            var filesToProcess = new List<string>();
            string basePath = Directory.GetCurrentDirectory();

            foreach (var relativeFile in options.FileList)
            {
                string absPath = Path.IsPathRooted(relativeFile)
                    ? relativeFile
                    : Path.Combine(basePath, relativeFile);

                if (File.Exists(absPath))
                {
                    logger.WriteLog($"File incluso dalla lista: {absPath}", "INFO");
                    filesToProcess.Add(absPath);
                }
                else
                {
                    logger.WriteLog($"File non trovato: {absPath}", "WARNING");
                    Console.WriteLine($"Avviso: File non trovato: {absPath}");
                }
            }

            return filesToProcess;
        }

        private static List<string> HandleExtensionsMode(CombineFilesOptions options, Logger logger, FileCollector collector)
        {
            var basePath = Directory.GetCurrentDirectory();
            var allFiles = collector.GetAllFiles(basePath, options.Recurse);
            var matched = new List<string>();

            foreach (var file in allFiles)
            {
                foreach (var ext in options.Extensions)
                {
                    if (file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    {
                        matched.Add(file);
                        break;
                    }
                }
            }

            matched = matched.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            logger.WriteLog($"File da processare dopo filtraggio per estensioni: {matched.Count}", "INFO");
            return matched;
        }

        private static List<string> HandleRegexMode(CombineFilesOptions options, Logger logger, FileCollector collector)
        {
            var basePath = Directory.GetCurrentDirectory();
            var allFiles = collector.GetAllFiles(basePath, options.Recurse);
            var matched = new List<string>();

            foreach (var file in allFiles)
            {
                foreach (var pattern in options.RegexPatterns)
                {
                    if (Regex.IsMatch(file, pattern))
                    {
                        matched.Add(file);
                        break; // Evita duplicati
                    }
                }
            }

            matched = matched.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            logger.WriteLog($"File da processare (regex): {matched.Count}", "INFO");
            return matched;
        }
    }
}