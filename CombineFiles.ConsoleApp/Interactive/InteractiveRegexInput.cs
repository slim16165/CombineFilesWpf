using System.Collections.Generic;
using Spectre.Console;

namespace CombineFiles.ConsoleApp.Interactive
{
    internal static class InteractiveRegexInput
    {
        public static List<string> GetRegexPatterns()
        {
            var patterns = new List<string>();
            while (AnsiConsole.Confirm("Add exclusion regex?"))
            {
                var regex = AnsiConsole.Ask<string>("Regex pattern:");
                patterns.Add(regex);
                AnsiConsole.MarkupLine($"[grey]{regex}[/] added.");
            }
            return patterns;
        }
    }
}