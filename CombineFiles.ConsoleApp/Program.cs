using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CombineFiles.ConsoleApp.Helpers;
using CombineFiles.Core;
using CombineFiles.Core.Configuration;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Infrastructure;
using CombineFiles.Core.Services;
using CommandLineBuilder = System.CommandLine.Builder.CommandLineBuilder;
using Logger = CombineFiles.Core.Infrastructure.Logger;

namespace CombineFiles.ConsoleApp
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = CreateRootCommand();

            // ESEMPIO: controlla collisioni e RIMUOVE gli alias duplicati
            // (invece di lanciare eccezioni)
            ParameterHelper.CheckAliasCollisions(
                rootCommand,
                skipConflictingAliases: true /* se false => lancia eccezione */
            );

            var commandLineBuilder = new CommandLineBuilder(rootCommand)
                .UseDefaults();

            var parser = commandLineBuilder.Build();
            return await parser.InvokeAsync(args);
        }

        /// <summary>
        /// Crea il comando radice e tutte le relative opzioni, associandole a un Binder personalizzato.
        /// </summary>
        private static RootCommand CreateRootCommand()
        {
            var rootCommand = new RootCommand("Strumento per combinare file seguendo diverse modalità di selezione");

            // Creazione di ogni opzione tramite un helper che genera automaticamente gli alias.
            // Per evitare molte collisioni, potresti abilitare meno alias "sinonimi"
            var helpOption = CreateOption<bool>("help", "Mostra l'aiuto", defaultValue: false, addDefaultValue: false, shortAlias: "h");
            var listPresetsOption = CreateOption<bool>("list-presets", "Mostra l'elenco dei preset disponibili", defaultValue: false, addDefaultValue: false, shortAlias: "l");
            var presetOption = CreateOption<string>("preset", "Specifica il preset da utilizzare", defaultValue: "", addDefaultValue: false, shortAlias: "p");

            var modeOption = CreateOption<string>("mode", "Modalità di selezione (list, extensions, regex, InteractiveSelection)", defaultValue: "", addDefaultValue: false, shortAlias: "m");

            var extensionsOption = CreateOption<List<string>>("extensions", "Elenco di estensioni da includere", defaultValue: null, addDefaultValue: false, shortAlias: null);
            var excludePathsOption = CreateOption<List<string>>("exclude-paths", "Percorsi da escludere", defaultValue: null, addDefaultValue: false, shortAlias: null);
            var excludeFilePatternsOption = CreateOption<List<string>>("exclude-file-patterns", "Pattern regex per escludere file", defaultValue: null, addDefaultValue: false, shortAlias: null);

            var outputFileOption = CreateOption("output-file", "File di output", "CombinedFile.txt", shortAlias: "o");
            var recurseOption = CreateOption<bool>("recurse", "Ricerca ricorsiva nelle sottocartelle", defaultValue: false, addDefaultValue: false);
            var enableLogOption = CreateOption<bool>("enable-log", "Abilita la generazione del log", defaultValue: false, addDefaultValue: false);


            // Aggiunta delle opzioni al comando radice
            rootCommand.AddOption(helpOption);
            rootCommand.AddOption(listPresetsOption);
            rootCommand.AddOption(presetOption);
            rootCommand.AddOption(modeOption);
            rootCommand.AddOption(extensionsOption);
            rootCommand.AddOption(excludePathsOption);
            rootCommand.AddOption(excludeFilePatternsOption);
            rootCommand.AddOption(outputFileOption);
            rootCommand.AddOption(recurseOption);
            rootCommand.AddOption(enableLogOption);

            // Impostiamo il gestore usando un binder personalizzato
            rootCommand.SetHandler(
                (CombineFilesOptions options) => Execute(options),
                new CombineFilesOptionsBinder(
                    helpOption,
                    listPresetsOption,
                    presetOption,
                    modeOption,
                    extensionsOption,
                    excludePathsOption,
                    excludeFilePatternsOption,
                    outputFileOption,
                    recurseOption,
                    enableLogOption));

            return rootCommand;
        }

        /// <summary>
        /// Entry point del programma, esegue la logica di combinazione dei file.
        /// Viene chiamato dal SetHandler del RootCommand.
        /// </summary>
        private static void Execute(CombineFilesOptions options)
        {
            if (HandleHelpAndPresets(options)) return;
            if (!ApplyPresetSafely(options)) return;

            var logger = InitializeLogger(options);

            if (!ValidateOptions(options, logger)) return;

            string sourcePath = Directory.GetCurrentDirectory();
            var normalizedExcludePaths = PathHelper.NormalizeExcludePaths(options.ExcludePaths, sourcePath, logger);

            ExcludeOutputFileIfNeeded(options, logger);

            var filesToProcess = CollectFiles(options, logger, normalizedExcludePaths, sourcePath);
            filesToProcess = FilterFiles(filesToProcess, options, logger);

            if (filesToProcess.Count == 0)
            {
                logger.WriteLog("Nessun file trovato per l'unione.", "WARNING");
                Console.WriteLine("Nessun file trovato per l'unione.");
                return;
            }

            if (!PrepareOutputFile(options, logger)) return;

            MergeFiles(filesToProcess, options, logger);
        }

        #region Metodi Helper di "Execute"

        private static bool HandleHelpAndPresets(CombineFilesOptions options)
        {
            if (options.Help)
            {
                ParameterHelper.PrintHelp();
                return true;
            }
            if (options.ListPresets)
            {
                ParameterHelper.PrintPresetList();
                return true;
            }
            return false;
        }

        private static bool ApplyPresetSafely(CombineFilesOptions options)
        {
            try
            {
                PresetManager.ApplyPreset(options);
                return true;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteColored(ex.Message, ConsoleColor.Red);
                return false;
            }
        }

        private static Logger InitializeLogger(CombineFilesOptions options)
        {
            string sourcePath = Directory.GetCurrentDirectory();
            string logFilePath = Path.Combine(sourcePath, "CombineFiles.log");
            var logger = new Logger(logFilePath, options.EnableLog);
            logger.WriteLog("Inizio operazione di combinazione file.", "INFO");
            return logger;
        }

        private static bool ValidateOptions(CombineFilesOptions options, Logger logger)
        {
            return ParameterHelper.ValidateParameters(options, logger);
        }

        private static void ExcludeOutputFileIfNeeded(CombineFilesOptions options, Logger logger)
        {
            if (!options.OutputToConsole && !string.IsNullOrWhiteSpace(options.OutputFile))
            {
                var outFileName = Path.GetFileName(options.OutputFile);
                if (!string.IsNullOrEmpty(outFileName))
                {
                    options.ExcludeFiles.Add(outFileName);
                    logger.WriteLog($"Aggiunto {outFileName} alla lista dei file esclusi (per evitare conflitti).", "DEBUG");
                }
            }
        }

        private static List<string> CollectFiles(
            CombineFilesOptions options,
            Logger logger,
            List<string> normalizedExcludePaths,
            string sourcePath)
        {
            logger.WriteLog("Inizio raccolta file...", "INFO");
            var fileCollector = new FileCollector(
                logger,
                normalizedExcludePaths,
                options.ExcludeFiles,
                options.ExcludeFilePatterns
            );
            var filesToProcess = FileCollectionHelper.CollectFiles(options, logger, fileCollector, sourcePath);
            logger.WriteLog($"Numero file iniziali: {filesToProcess.Count}", "INFO");
            return filesToProcess;
        }

        private static List<string> FilterFiles(
            List<string> files,
            CombineFilesOptions options,
            Logger logger)
        {
            var filtered = FileFilterHelper.FilterByDateAndSize(files, options, logger);

            // Escludiamo i file con tag <auto-generated>
            filtered = filtered.Where(f => !FileFilterHelper.FileContainsAutoGenerated(f, logger)).ToList();
            logger.WriteLog($"Totale file dopo esclusione 'auto-generated': {filtered.Count}", "INFO");

            return filtered;
        }

        private static bool PrepareOutputFile(CombineFilesOptions options, Logger logger)
        {
            if (options.OutputToConsole || string.IsNullOrWhiteSpace(options.OutputFile))
                return true;

            try
            {
                Encoding selectedEncoding = Encoding.UTF8;
                OutputFileHelper.PrepareOutputFile(options.OutputFile, options.OutputFormat, selectedEncoding, logger);
                return true;
            }
            catch (Exception ex)
            {
                logger.WriteLog($"Impossibile creare/scrivere il file di output: {ex.Message}", "ERROR");
                Console.WriteLine($"Errore: Impossibile creare/scrivere nel file di output: {ex.Message}");
                return false;
            }
        }

        private static void MergeFiles(List<string> filesToProcess, CombineFilesOptions options, Logger logger)
        {
            var fileMerger = new FileMerger(
                logger,
                options.OutputToConsole,
                options.OutputFile,
                options.OutputFormat,
                options.FileNamesOnly
            );
            fileMerger.MergeFiles(filesToProcess);

            if (!options.OutputToConsole)
            {
                logger.WriteLog($"Operazione completata. Controlla il file '{options.OutputFile}'.", "INFO");
                Console.WriteLine($"Operazione completata. Controlla il file '{options.OutputFile}'.");
            }
            else
            {
                logger.WriteLog("Operazione completata con output a console.", "INFO");
                Console.WriteLine("Operazione completata con output a console.");
            }
        }

        #endregion

        #region Funzioni di creazione Opzioni e Binder

        /// <summary>
        /// Crea una Option<T> con il solo alias lungo "--baseName".
        /// Se `shortAlias` non è null o vuoto, aggiunge anche quello.
        /// In più, se baseName inizia con 'e', ignora del tutto la creazione del shortAlias 
        /// per evitare collisioni.
        /// </summary>
        private static Option<T> CreateOption<T>(
            string baseName,
            string description,
            T defaultValue = default!,
            bool addDefaultValue = false,
            string? shortAlias = null)
        {
            // 1) Genera l'alias lungo baseName (es. "--mode")
            var aliases = new List<string> { $"--{baseName}" };

            // 2) Se la baseName inizia con 'e', forza l'eliminazione di shortAlias
            // (per evitare collisioni).
            if (!string.IsNullOrEmpty(baseName) && baseName.StartsWith("e", StringComparison.OrdinalIgnoreCase))
            {
                shortAlias = null;
            }

            // 3) Se shortAlias è esplicitamente richiesto, e non è già presente, aggiungilo.
            if (!string.IsNullOrEmpty(shortAlias))
            {
                if (!shortAlias.StartsWith("-"))
                    shortAlias = "-" + shortAlias;

                // Aggiungi il -x tra gli alias, se non duplicato
                if (!aliases.Contains(shortAlias))
                    aliases.Add(shortAlias);
            }

            // 4) Crea la option con o senza default
            Option<T> option;
            if (addDefaultValue)
            {
                option = new Option<T>(aliases.ToArray(), () => defaultValue, description);
            }
            else
            {
                option = new Option<T>(aliases.ToArray(), description);
            }

            return option;
        }

        /// <summary>
        /// Overload per i casi in cui vogliamo sempre un default value (più leggibile).
        /// </summary>
        private static Option<T> CreateOption<T>(
            string baseName,
            string description,
            T defaultValue,
            string? shortAlias = null)
        {
            return CreateOption(baseName, description, defaultValue, addDefaultValue: true, shortAlias);
        }

        #endregion
    }
}
