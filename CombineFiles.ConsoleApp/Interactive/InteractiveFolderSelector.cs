using System.Collections.Generic;
using System.IO;
using System.Linq;
using CombineFiles.Core.Helpers;
using Spectre.Console;

namespace CombineFiles.ConsoleApp.Interactive
{
    internal static class InteractiveFolderSelector
    {
        public static List<string> SelectExcludedPaths()
        {
            var basePath = Directory.GetCurrentDirectory();
            var dirs = Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories)
                .Select(p => FileHelper.GetRelativePath(basePath, p))
                .OrderBy(p => p)
                .ToList();

            var selected = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select directories to [red]exclude[/] (space to toggle):")
                    .AddChoices(dirs));

            // Convert back to absolute
            return selected.Select(p => Path.Combine(basePath, p)).ToList();
        }

        /// <summary>
        /// Permette di selezionare cartelle da includere ed escludere in modo interattivo.
        /// </summary>
        public static (List<string> includedPaths, List<string> excludedPaths) SelectIncludedAndExcludedPaths()
        {
            var basePath = Directory.GetCurrentDirectory();
            var allDirs = Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories)
                .Select(p => FileHelper.GetRelativePath(basePath, p))
                .OrderBy(p => p)
                .ToList();

            AnsiConsole.MarkupLine("[yellow]Selezione Cartelle Avanzata[/]");
            AnsiConsole.MarkupLine("Puoi selezionare cartelle da [green]includere[/] o [red]escludere[/].");
            AnsiConsole.WriteLine();

            // Prima chiedi se vuoi includere o escludere
            var mode = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Come vuoi procedere?")
                    .AddChoices(new[] { "Includi cartelle specifiche", "Escludi cartelle specifiche", "Entrambe" }));

            var includedPaths = new List<string>();
            var excludedPaths = new List<string>();

            if (mode == "Includi cartelle specifiche" || mode == "Entrambe")
            {
                var included = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<string>()
                        .Title("Seleziona cartelle da [green]includere[/] (space to toggle, Enter to confirm):")
                        .AddChoices(allDirs)
                        .NotRequired());

                includedPaths = included.Select(p => Path.Combine(basePath, p)).ToList();
            }

            if (mode == "Escludi cartelle specifiche" || mode == "Entrambe")
            {
                // Se abbiamo già selezionato cartelle da includere, mostra solo quelle non incluse
                var availableForExclusion = mode == "Entrambe" && includedPaths.Count > 0
                    ? allDirs.Where(d => !includedPaths.Contains(Path.Combine(basePath, d))).ToList()
                    : allDirs;

                if (availableForExclusion.Count > 0)
                {
                    var excluded = AnsiConsole.Prompt(
                        new MultiSelectionPrompt<string>()
                            .Title("Seleziona cartelle da [red]escludere[/] (space to toggle, Enter to confirm):")
                            .AddChoices(availableForExclusion)
                            .NotRequired());

                    excludedPaths = excluded.Select(p => Path.Combine(basePath, p)).ToList();
                }
            }

            return (includedPaths, excludedPaths);
        }
    }
}