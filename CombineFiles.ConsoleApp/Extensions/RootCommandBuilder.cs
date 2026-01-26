using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using CombineFiles.ConsoleApp.Helpers;
using CombineFiles.ConsoleApp.Interactive;
using CombineFiles.Core.Configuration;

namespace CombineFiles.ConsoleApp.Extensions;

/// <summary>
/// Classe dedicata a creare e configurare il RootCommand, con tutte le opzioni e il relativo Binder.
/// </summary>
public static class RootCommandBuilder
{
    public static RootCommand CreateRootCommand()
    {

        // Utilizzo del builder pattern per creare le opzioni in modo fluido
        var helpOption = OptionBuilder.For<bool>("Help")
            .WithDescription("Mostra l'aiuto")
            .WithShortAlias("h")
            .Build();

        var listPresetsOption = OptionBuilder.For<bool>("List-presets")
            .WithDescription("Mostra l'elenco dei preset disponibili")
            .WithShortAlias("l")
            .Build();

        var presetOption = OptionBuilder.For<string>("Preset")
            .WithDescription("Specifica il preset da utilizzare")
            .WithShortAlias("p")
            .Build();

        var modeOption = OptionBuilder.For<string>("Mode")
            .WithDescription("Modalità di selezione (list, extensions, regex, InteractiveSelection)")
            .WithShortAlias("m")
            .Build();

        // Per le opzioni che iniziano con 'e' il builder ignora il short alias
        var extensionsOption = OptionBuilder.For<List<string>>("Extensions")
            .WithDescription("Elenco di estensioni da includere")
            .Build();

        var excludePathsOption = OptionBuilder.For<List<string>>("exclude-paths")
            .WithDescription("Percorsi da escludere")
            .Build();

        var excludeFilePatternsOption = OptionBuilder.For<List<string>>("Exclude-file-patterns")
            .WithDescription("Pattern regex per escludere file")
            .Build();

        var outputFileOption = OptionBuilder.For<string>("Output-file")
            .WithDescription("File di output")
            .WithDefaultValue("CombinedFile.txt")
            .WithShortAlias("o")
            .Build();

        var recurseOption = OptionBuilder.For<bool>("Recurse")
            .WithDescription("Ricerca ricorsiva nelle sottocartelle")
            .Build();

        var enableLogOption = OptionBuilder.For<bool>("Enable-log")
            .WithDescription("Abilita la generazione del log")
            .Build();
        var interactiveOpt = OptionBuilder.For<bool>("Interactive").WithDescription("Launch interactive configuration wizard").WithShortAlias("i").Build();

        var maxTokensOption = OptionBuilder.For<int>("Max-tokens")
            .WithDescription("Limite massimo di token da processare (0 = illimitato)")
            .WithDefaultValue(0)
            .Build();

        var partialFileModeOption = OptionBuilder.For<string>("Partial-file-mode")
            .WithDescription("Strategia per file che superano il limite token: 'exclude' (default) o 'partial'")
            .WithDefaultValue("partial")
            .Build();

        var debugOption = OptionBuilder.For<bool>("Debug")
            .WithDescription("Abilita la modalità di debug dettagliata")
            .WithShortAlias("d")
            .Build();

        var compactSpacesOption = OptionBuilder.For<bool>("Compact-spaces")
            .WithDescription("Converte 4 spazi consecutivi in 1 tab per ridurre i token")
            .Build();

        var compactLLMOption = OptionBuilder.For<bool>("Compact-llm")
            .WithDescription("Ottimizza l'output per LLM: rimuove righe vuote eccessive e compatta header")
            .Build();

        //while (!Debugger.IsAttached) Thread.Sleep(100);
        //Debugger.Break();

        // Creazione del RootCommand (System.CommandLine 2.0+)
        var rootCommand = new RootCommand("Strumento per combinare file seguendo diverse modalità di selezione");
        
        // Aggiunta delle opzioni (System.CommandLine 2.0+ usa Options.Add invece di AddOption)
        rootCommand.Options.Add(helpOption);
        rootCommand.Options.Add(listPresetsOption);
        rootCommand.Options.Add(presetOption);
        rootCommand.Options.Add(modeOption);
        rootCommand.Options.Add(extensionsOption);
        rootCommand.Options.Add(excludePathsOption);
        rootCommand.Options.Add(excludeFilePatternsOption);
        rootCommand.Options.Add(outputFileOption);
        rootCommand.Options.Add(recurseOption);
        rootCommand.Options.Add(enableLogOption);
        rootCommand.Options.Add(interactiveOpt);
        rootCommand.Options.Add(maxTokensOption);
        rootCommand.Options.Add(partialFileModeOption);
        rootCommand.Options.Add(debugOption);
        rootCommand.Options.Add(compactSpacesOption);
        rootCommand.Options.Add(compactLLMOption);

        // Imposta il gestore con SetAction (System.CommandLine 2.0+ usa SetAction invece di SetHandler)
        rootCommand.SetAction(parseResult =>
        {
            // Estrai i valori dalle opzioni usando ParseResult
            bool help = parseResult.GetValue(helpOption);
            bool listPresets = parseResult.GetValue(listPresetsOption);
            string? preset = parseResult.GetValue(presetOption);
            string? mode = parseResult.GetValue(modeOption);
            List<string>? extensions = parseResult.GetValue(extensionsOption);
            List<string>? excludePaths = parseResult.GetValue(excludePathsOption);
            List<string>? excludeFilePatterns = parseResult.GetValue(excludeFilePatternsOption);
            string? outputFile = parseResult.GetValue(outputFileOption);
            bool recurse = parseResult.GetValue(recurseOption);
            bool enableLog = parseResult.GetValue(enableLogOption);
            bool interactive = parseResult.GetValue(interactiveOpt);
            int maxTokens = parseResult.GetValue(maxTokensOption);
            string? partialFileMode = parseResult.GetValue(partialFileModeOption);
            bool compactSpaces = parseResult.GetValue(compactSpacesOption);
            bool compactLLM = parseResult.GetValue(compactLLMOption);

            var options = CombineFilesOptionsBinder.CreateOptions(
                help, listPresets, preset, mode, extensions, excludePaths, excludeFilePatterns,
                outputFile, recurse, enableLog, interactive, maxTokens, partialFileMode,
                compactSpaces, compactLLM);

            // If --interactive flag is present, open Spectre.Console wizard
            if (options.Interactive)
            {
                options = InteractiveMode.Run();
            }

            ExecutionFlow.Execute(options);
        });

        return rootCommand;
    }
}