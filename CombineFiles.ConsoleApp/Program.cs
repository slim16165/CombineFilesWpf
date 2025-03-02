using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CombineFiles.ConsoleApp.Core;
using CombineFiles.ConsoleApp.Helpers;
using CombineFiles.Core.Configuration;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.ConsoleApp
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = CreateRootCommand();

            var commandLineBuilder = new CommandLineBuilder(rootCommand);
            commandLineBuilder.UseDefaults();

            var parser = commandLineBuilder.Build();
            return await parser.InvokeAsync(args);
        }

        private static RootCommand CreateRootCommand()
        {
            var rootCommand = new RootCommand("Strumento per combinare file seguendo diverse modalità di selezione");

            // Aggiungi le opzioni direttamente al rootCommand
            rootCommand.AddOption(new Option<bool>(["--help", "-h"], "Mostra l'aiuto"));
            rootCommand.AddOption(new Option<bool>("--list-presets", "Mostra l'elenco dei preset disponibili"));
            rootCommand.AddOption(new Option<string>(["--preset", "-p"], "Specifica il preset da utilizzare"));
            rootCommand.AddOption(new Option<string>(["--mode", "-m"], "Modalità di selezione (list, extensions, regex, InteractiveSelection)"));
            rootCommand.AddOption(new Option<List<string>>(["--extensions", "-e"], "Elenco di estensioni da includere"));
            rootCommand.AddOption(new Option<List<string>>(["--exclude-paths", "-ep"], "Percorsi da escludere"));
            rootCommand.AddOption(new Option<List<string>>(["--exclude-file-patterns", "-efp"], "Pattern regex per escludere file"));
            rootCommand.AddOption(new Option<string>(["--output-file", "-o"], () => "CombinedFile.txt", "File di output"));
            rootCommand.AddOption(new Option<bool>("--recurse", "Ricerca ricorsiva nelle sottocartelle"));
            rootCommand.AddOption(new Option<bool>("--enable-log", "Abilita la generazione del log"));

            // Imposta il gestore del comando radice
            rootCommand.SetHandler((CombineFilesOptions options) => Execute(options), new CombineFilesOptionsBinder());

            return rootCommand;
        }


        public static void Execute(CombineFilesOptions options)
        {
            // 1) Gestione help
            if (options.Help)
            {
                ParameterHelper.PrintHelp();
                return;
            }

            // 2) Gestione LIST PRESETS
            if (options.ListPresets)
            {
                ParameterHelper.PrintPresetList();
                return;
            }

            // 3) Applica eventuale preset
            try
            {
                PresetManager.ApplyPreset(options);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                return;
            }

            // 4) Inizializza il logger
            string sourcePath = Directory.GetCurrentDirectory();
            string logFilePath = Path.Combine(sourcePath, "CombineFiles.log");
            var logger = new Logger(logFilePath, options.EnableLog);
            logger.WriteLog("Inizio operazione di combinazione file.", "INFO");

            // 5) Validazione parametri
            if (!ParameterHelper.ValidateParameters(options, logger))
                return;

            // 6) Normalizza exclude paths
            var normalizedExcludePaths = PathHelper.NormalizeExcludePaths(options.ExcludePaths, sourcePath, logger);

            // 7) Escludi il file di output, se non si scrive in console
            if (!options.OutputToConsole && !string.IsNullOrWhiteSpace(options.OutputFile))
            {
                var outFileName = Path.GetFileName(options.OutputFile);
                if (!string.IsNullOrEmpty(outFileName))
                {
                    options.ExcludeFiles.Add(outFileName);
                    logger.WriteLog($"Aggiunto {outFileName} alla lista dei file esclusi (per evitare conflitti).", "DEBUG");
                }
            }

            // 8) Raccolta file (in base a Mode)
            logger.WriteLog("Inizio raccolta file...", "INFO");
            var fileCollector = new FileCollector(
                logger,
                normalizedExcludePaths,
                options.ExcludeFiles,
                options.ExcludeFilePatterns
            );

            List<string> filesToProcess = FileCollectionHelper.CollectFiles(options, logger, fileCollector, sourcePath);
            logger.WriteLog($"Numero file iniziali: {filesToProcess.Count}", "INFO");

            // 9) Filtri su date e dimensioni
            filesToProcess = FileFilterHelper.FilterByDateAndSize(filesToProcess, options, logger);

            // 10) Filtro eventuali file <auto-generated>
            filesToProcess = filesToProcess
                .Where(f => !FileFilterHelper.FileContainsAutoGenerated(f, logger))
                .ToList();

            logger.WriteLog($"Totale file dopo esclusione 'auto-generated': {filesToProcess.Count}", "INFO");

            // 11) Verifica se ci sono file da processare
            if (filesToProcess.Count == 0)
            {
                logger.WriteLog("Nessun file trovato per l'unione.", "WARNING");
                Console.WriteLine("Nessun file trovato per l'unione.");
                return;
            }

            // 12) Scegli encoding (semplice: UTF8)
            Encoding selectedEncoding = Encoding.UTF8;

            // 13) Prepara output file
            if (!options.OutputToConsole && !string.IsNullOrWhiteSpace(options.OutputFile))
            {
                try
                {
                    OutputFileHelper.PrepareOutputFile(options.OutputFile, options.OutputFormat, selectedEncoding, logger);
                }
                catch (Exception ex)
                {
                    logger.WriteLog($"Impossibile creare/scrivere il file di output: {ex.Message}", "ERROR");
                    Console.WriteLine($"Errore: Impossibile creare/scrivere nel file di output: {ex.Message}");
                    return;
                }
            }

            // 14) Merge finale
            var fileMerger = new FileMerger(
                logger,
                options.OutputToConsole,
                options.OutputFile,
                options.OutputFormat,
                options.FileNamesOnly
            );
            fileMerger.MergeFiles(filesToProcess);

            // Fine
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
    }

    // Classe per il binding delle opzioni
    public class CombineFilesOptionsBinder : BinderBase<CombineFilesOptions>
    {
        protected override CombineFilesOptions GetBoundValue(BindingContext bindingContext)
        {
            return new CombineFilesOptions
            {
                Help = bindingContext.ParseResult.GetValueForOption(new Option<bool>("--help")),
                ListPresets = bindingContext.ParseResult.GetValueForOption(new Option<bool>("--list-presets")),
                Preset = bindingContext.ParseResult.GetValueForOption(new Option<string>("--preset")),
                Mode = bindingContext.ParseResult.GetValueForOption(new Option<string>("--mode")),
                Extensions = bindingContext.ParseResult.GetValueForOption(new Option<List<string>>("--extensions")) ?? new List<string>(),
                ExcludePaths = bindingContext.ParseResult.GetValueForOption<List<string>>(new Option<List<string>>("--exclude-paths")) ?? new List<string>(),
                ExcludeFilePatterns = bindingContext.ParseResult.GetValueForOption<List<string>>(new Option<List<string>>("--exclude-file-patterns")) ?? new List<string>(),
                OutputFile = bindingContext.ParseResult.GetValueForOption<string>(new Option<string>("--output-file")),
                Recurse = bindingContext.ParseResult.GetValueForOption(new Option<bool>("--recurse")),
                EnableLog = bindingContext.ParseResult.GetValueForOption(new Option<bool>("--enable-log"))
            };
        }
    }
}