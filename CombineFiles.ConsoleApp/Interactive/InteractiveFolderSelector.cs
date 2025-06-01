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
    }
}