using System;
using System.Linq;
using System.Text.Json;
using CombineFiles.Core;
using CombineFiles.Core.Configuration;
using Spectre.Console;

namespace CombineFiles.ConsoleApp.Interactive;

/// <summary>
/// Full‑screen interactive wizard powered by Spectre.Console.
/// Returns a fully populated <see cref="CombineFilesOptions"/> ready for execution.
/// </summary>
public static class InteractiveMode
{
    public static CombineFilesOptions Run()
    {
        var options = new CombineFilesOptions();

        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("CombineFiles").Color(Color.Lime));
        AnsiConsole.WriteLine();

        options.Mode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Select operating mode:[/]")
                .AddChoices("extensions", "list", "regex", "interactiveSelection"));

        // Preset loading
        if (AnsiConsole.Confirm("Load a preset?"))
        {
            var presetName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Select preset:[/]")
                    .AddChoices(PresetManager.Presets.Keys));
            options.Preset = presetName;
            PresetManager.ApplyPreset(options);
        }

        // Extensions (if not already set by preset)
        if (options.Mode == "extensions")
        {
            var extInput = AnsiConsole.Ask<string>("Extensions to include (comma separated, blank = ALL):");
            if (!string.IsNullOrWhiteSpace(extInput))
                options.Extensions = extInput.Split([','], StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim().StartsWith(".") ? e.Trim() : "." + e.Trim())
                    .ToList();
        }

        // Interactive folder selector for inclusions/exclusions
        if (AnsiConsole.Confirm("Selezionare cartelle da includere/escludere?"))
        {
            var (included, excluded) = InteractiveFolderSelector.SelectIncludedAndExcludedPaths();
            options.IncludePaths = included;
            options.ExcludePaths = excluded;
        }
        else if (AnsiConsole.Confirm("Interactively exclude sub‑folders?"))
        {
            options.ExcludePaths = InteractiveFolderSelector.SelectExcludedPaths();
        }

        // Advanced section
        if (AnsiConsole.Confirm("Configure advanced filters?"))
        {
            options.MinSize = AnsiConsole.Ask<string>("Min file size (e.g. 0, 10KB, 2MB):", options.MinSize);
            options.MaxSize = AnsiConsole.Ask<string>("Max file size (blank = unlimited):", options.MaxSize);
            options.MaxLinesPerFile =
                AnsiConsole.Ask<int>("Max lines per file (0 = unlimited):", options.MaxLinesPerFile);
            options.MaxTotalTokens =
                AnsiConsole.Ask<int>("Global token budget (0 = unlimited):", options.MaxTotalTokens);

            if (AnsiConsole.Confirm("Add regex exclusion patterns?"))
                options.RegexPatterns = InteractiveRegexInput.GetRegexPatterns();
        }

        // Output handling
        options.OutputToConsole = AnsiConsole.Confirm("Output to console? (No = write to file)");
        if (!options.OutputToConsole)
            options.OutputFile = AnsiConsole.Ask<string>("Output file path:", options.OutputFile);

        options.ListOnlyFileNames = AnsiConsole.Confirm("Only list file names (no content)?");

        // Final confirmation
        AnsiConsole.Write(new Rule("Review").RuleStyle("grey"));
        AnsiConsole.WriteLine(JsonSerializer.Serialize(options));
        if (!AnsiConsole.Confirm("Proceed with these settings?"))
        {
            AnsiConsole.MarkupLine("[red]Aborted by user.[/]");
            Environment.Exit(1);
        }

        return options;
    }
}