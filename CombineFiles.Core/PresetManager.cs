using System;
using System.Collections.Generic;
using CombineFiles.Core.Configuration;

namespace CombineFiles.ConsoleApp;

public static class PresetManager
{
    public static readonly Dictionary<string?, Dictionary<string, object>> Presets =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["CSharp"] = new Dictionary<string, object>
            {
                { "Mode", "extensions" },
                { "Extensions", new List<string>{ ".cs", ".xaml" } },
                { "OutputFile", "CombinedFile.cs" },
                { "Recurse", true },
                { "ExcludePaths", new List<string>{ "Properties", "obj", "bin" } },
                { "ExcludeFilePatterns", new List<string>{ ".*\\.g\\.i\\.cs$", ".*\\.g\\.cs$", ".*\\.designer\\.cs$", ".*AssemblyInfo\\.cs$", "^auto-generated" } }
            },
            // Esempio di preset per VB (ipotetico)
            ["VB"] = new Dictionary<string, object>
            {
                { "Mode", "extensions" },
                { "Extensions", new List<string>{ ".vb", ".resx" } },
                { "OutputFile", "CombinedFile.vb" },
                { "Recurse", true },
                { "ExcludePaths", new List<string>{ "My Project", "bin", "obj" } },
                { "ExcludeFilePatterns", new List<string>{ ".*\\.designer\\.vb$", ".*AssemblyInfo\\.vb$" } }
            },
            // Esempio di preset per JavaScript (ipotetico)
            ["JavaScript"] = new Dictionary<string, object>
            {
                { "Mode", "extensions" },
                { "Extensions", new List<string>{ ".js", ".jsx" } },
                { "OutputFile", "CombinedFile.js" },
                { "Recurse", true },
                { "ExcludePaths", new List<string>{ "node_modules", "dist" } },
                { "ExcludeFilePatterns", new List<string>{ ".*\\.min\\.js$" } }
            }
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
                        {
                            options.Mode = val.ToString();
                            Console.WriteLine($"Applicato preset '{options.Preset}': Mode = {options.Mode}");
                        }
                        break;
                    case "extensions":
                        if (options.Extensions.Count == 0)
                        {
                            options.Extensions = (List<string>)val;
                            Console.WriteLine($"Applicato preset '{options.Preset}': Extensions = {string.Join(" ", options.Extensions)}");
                        }
                        break;
                    case "outputfile":
                        if (options.OutputFile == "CombinedFile.txt")
                        {
                            options.OutputFile = val.ToString();
                            Console.WriteLine($"Applicato preset '{options.Preset}': OutputFile = {options.OutputFile}");
                        }
                        break;
                    case "recurse":
                        if (!options.Recurse)
                        {
                            options.Recurse = (bool)val;
                            Console.WriteLine($"Applicato preset '{options.Preset}': Recurse = {options.Recurse}");
                        }
                        break;
                    case "excludepaths":
                        if (options.ExcludePaths.Count == 0)
                        {
                            options.ExcludePaths = (List<string>)val;
                            Console.WriteLine($"Applicato preset '{options.Preset}': ExcludePaths = {string.Join(" ", options.ExcludePaths)}");
                        }
                        break;
                    case "excludefilepatterns":
                        if (options.ExcludeFilePatterns.Count == 0)
                        {
                            options.ExcludeFilePatterns = (List<string>)val;
                            Console.WriteLine($"Applicato preset '{options.Preset}': ExcludeFilePatterns = {string.Join(" ", options.ExcludeFilePatterns)}");
                        }
                        break;
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