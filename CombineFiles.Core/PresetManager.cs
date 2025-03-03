using System;
using System.Collections.Generic;
using CombineFiles.Core.Configuration;
#pragma warning disable CS8604 // Possible null reference argument.

namespace CombineFiles.ConsoleApp
{
    // Preset immutabile con proprietà di sola lettura.
    public class Preset
    {
        public string Mode { get; }
        public IReadOnlyList<string> Extensions { get; }
        public string OutputFile { get; }
        public bool Recurse { get; }
        public IReadOnlyList<string> ExcludePaths { get; }
        public IReadOnlyList<string> ExcludeFilePatterns { get; }

        public Preset(
            string mode,
            List<string> extensions,
            string outputFile,
            bool recurse,
            List<string> excludePaths,
            List<string> excludeFilePatterns)
        {
            Mode = mode;
            Extensions = extensions.AsReadOnly();
            OutputFile = outputFile;
            Recurse = recurse;
            ExcludePaths = excludePaths.AsReadOnly();
            ExcludeFilePatterns = excludeFilePatterns.AsReadOnly();
        }
    }

    public static class PresetManager
    {
        // I preset sono definiti in modo immutabile e possono essere eventualmente spostati in un file di configurazione.
        public static readonly Dictionary<string, Preset> Presets =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["CSharp"] = new Preset(
                    mode: "extensions",
                    extensions: [".cs", ".xaml"],
                    outputFile: "CombinedFile.cs",
                    recurse: true,
                    excludePaths: ["Properties", "obj", "bin"],
                    excludeFilePatterns:
                    [
                        ".*\\.g\\.i\\.cs$", ".*\\.g\\.cs$", ".*\\.designer\\.cs$", ".*AssemblyInfo\\.cs$",
                        "^auto-generated"
                    ]
                ),
                ["VB"] = new Preset(
                    mode: "extensions",
                    extensions: [".vb", ".resx"],
                    outputFile: "CombinedFile.vb",
                    recurse: true,
                    excludePaths: ["My Project", "bin", "obj"],
                    excludeFilePatterns: [".*\\.designer\\.vb$", ".*AssemblyInfo\\.vb$"]
                ),
                ["JavaScript"] = new Preset(
                    mode: "extensions",
                    extensions: [".js", ".jsx"],
                    outputFile: "CombinedFile.js",
                    recurse: true,
                    excludePaths: ["node_modules", "dist"],
                    excludeFilePatterns: [".*\\.min\\.js$"]
                )
            };

        public static void ApplyPreset(CombineFilesOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Preset))
                return;

            if (!Presets.TryGetValue(options.Preset, out Preset? preset))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Errore: Preset '{options.Preset}' non trovato.");
                Console.ResetColor();
                throw new ArgumentException($"Preset '{options.Preset}' non trovato");
            }

            // Usa il mapper per applicare il preset alle opzioni
            OptionsMapper.Map(preset, options);
        }
    }

    // Classe dedicata alla mappatura dei preset sulle opzioni,
    // centralizzando così la logica e rendendola più facilmente testabile.
    public static class OptionsMapper
    {
        public static void Map(Preset preset, CombineFilesOptions options)
        {
            if (string.IsNullOrEmpty(options.Mode))
            {
                options.Mode = preset.Mode;
                LogMapping("Mode", options.Preset, preset.Mode);
            }

            if (options.Extensions.Count == 0)
            {
                options.Extensions = new List<string>(preset.Extensions);
                LogMapping("Extensions", options.Preset, string.Join(" ", preset.Extensions));
            }

            // Supponiamo che CombineFilesOptions.DefaultOutputFile sia "CombinedFile.txt"
            if (options.OutputFile == CombineFilesOptions.DefaultOutputFile)
            {
                options.OutputFile = preset.OutputFile;
                LogMapping("OutputFile", options.Preset, preset.OutputFile);
            }

            if (!options.Recurse)
            {
                options.Recurse = preset.Recurse;
                LogMapping("Recurse", options.Preset, preset.Recurse.ToString());
            }

            if (options.ExcludePaths.Count == 0)
            {
                options.ExcludePaths = new List<string>(preset.ExcludePaths);
                LogMapping("ExcludePaths", options.Preset, string.Join(" ", preset.ExcludePaths));
            }

            if (options.ExcludeFilePatterns.Count == 0)
            {
                options.ExcludeFilePatterns = new List<string>(preset.ExcludeFilePatterns);
                LogMapping("ExcludeFilePatterns", options.Preset, string.Join(" ", preset.ExcludeFilePatterns));
            }
        }

        private static void LogMapping(string propertyName, string presetName, string value)
        {
            Console.WriteLine($"Applicato preset '{presetName}': {propertyName} = {value}");
        }
    }
}
