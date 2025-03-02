using System;
using System.Collections.Generic;

namespace CombineFiles.ConsoleApp;

public static class PresetManager
{
    public static readonly Dictionary<string, Dictionary<string, object>> Presets =
        new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase)
        {
            ["CSharp"] = new Dictionary<string, object>
            {
                { "Mode", "extensions" },
                { "Extensions", new List<string>{ ".cs", ".xaml" } },
                { "OutputFile", "CombinedFile.cs" },
                { "Recurse", true },
                { "ExcludePaths", new List<string>{ "Properties", "obj", "bin" } },
                { "ExcludeFilePatterns", new List<string>{ ".*\\.g\\.i\\.cs$", ".*\\.g\\.cs$", ".*\\.designer\\.cs$", ".*AssemblyInfo\\.cs$", "^auto-generated" } }
            }
            // Altri preset…
        };

    public static void ApplyPreset(CombineFilesOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Preset) &&
            Presets.TryGetValue(options.Preset, out var presetParams))
        {
            foreach (var kvp in presetParams)
            {
                string key = kvp.Key;
                object val = kvp.Value;

                switch (key.ToLowerInvariant())
                {
                    case "mode":
                        if (string.IsNullOrEmpty(options.Mode))
                            options.Mode = val.ToString();
                        break;
                    case "extensions":
                        if (options.Extensions == null || options.Extensions.Count == 0)
                            options.Extensions = new List<string>((List<string>)val);
                        break;
                    case "outputfile":
                        if (options.OutputFile == "CombinedFile.txt")
                            options.OutputFile = val.ToString();
                        break;
                    case "recurse":
                        if (!options.Recurse)
                            options.Recurse = (bool)val;
                        break;
                    case "excludepaths":
                        if (options.ExcludePaths == null || options.ExcludePaths.Count == 0)
                            options.ExcludePaths = new List<string>((List<string>)val);
                        break;
                    case "excludefilepatterns":
                        if (options.ExcludeFilePatterns == null || options.ExcludeFilePatterns.Count == 0)
                            options.ExcludeFilePatterns = new List<string>((List<string>)val);
                        break;
                    // Altri campi…
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(options.Preset))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Errore: Preset '{options.Preset}' non trovato.");
            Console.ResetColor();
            throw new ArgumentException($"Preset '{options.Preset}' non trovato");
        }
    }
}