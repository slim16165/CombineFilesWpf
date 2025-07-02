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
        var rootCommand = new RootCommand("Strumento per combinare file seguendo diverse modalità di selezione");

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
            .WithDefaultValue("exclude")
            .Build();

        var debugOption = OptionBuilder.For<bool>("Debug")
            .WithDescription("Abilita la modalità di debug dettagliata")
            .WithShortAlias("d")
            .Build();

        //while (!Debugger.IsAttached) Thread.Sleep(100);
        //Debugger.Break();

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
        rootCommand.AddOption(interactiveOpt);
        rootCommand.AddOption(maxTokensOption);
        rootCommand.AddOption(partialFileModeOption);
        rootCommand.AddOption(debugOption);

        // Imposta il gestore con un binder personalizzato
        rootCommand.SetHandler(
            (CombineFilesOptions options) =>
            {
                // If --interactive flag is present, open Spectre.Console wizard
                if (options.Interactive)
                {
                    options = InteractiveMode.Run();
                }

                ExecutionFlow.Execute(options);
            },
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
                enableLogOption,
                interactiveOpt,
                maxTokensOption,
                partialFileModeOption,
                debugOption));

        return rootCommand;
    }
}