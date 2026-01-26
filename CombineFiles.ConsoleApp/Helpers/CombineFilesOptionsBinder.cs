using System.Collections.Generic;
using System.CommandLine;
using CombineFiles.Core.Configuration;
using CombineFiles.Core.Services;

namespace CombineFiles.ConsoleApp.Helpers;

/// <summary>
/// Helper per creare CombineFilesOptions dai valori delle opzioni.
/// In System.CommandLine 2.0+, il binding viene fatto direttamente nel SetHandler.
/// </summary>
public static class CombineFilesOptionsBinder
{
    /// <summary>
    /// Metodo di convenienza per creare CombineFilesOptions usando il builder pattern.
    /// Mantiene la retrocompatibilità con il codice esistente.
    /// </summary>
    public static CombineFilesOptions CreateOptions(
        bool help,
        bool listPresets,
        string? preset,
        string? mode,
        List<string>? extensions,
        List<string>? excludePaths,
        List<string>? excludeFilePatterns,
        string? outputFile,
        bool recurse,
        bool enableLog,
        bool interactive,
        int maxTokens,
        string? partialFileMode,
        bool compactSpaces,
        bool compactLLM,
        int tokenWarningThreshold,
        bool autoPaginate)
    {
        return new CombineFilesOptionsBuilder()
            .WithHelp(help)
            .WithListPresets(listPresets)
            .WithPreset(preset)
            .WithMode(mode)
            .WithExtensions(extensions)
            .WithExcludePaths(excludePaths)
            .WithExcludeFilePatterns(excludeFilePatterns)
            .WithOutputFile(outputFile)
            .WithRecurse(recurse)
            .WithEnableLog(enableLog)
            .WithInteractive(interactive)
            .WithMaxTokens(maxTokens)
            .WithPartialFileMode(partialFileMode)
            .WithCompactSpaces(compactSpaces)
            .WithCompactLLM(compactLLM)
            .WithTokenWarningThreshold(tokenWarningThreshold)
            .WithAutoPaginateOnThreshold(autoPaginate)
            .Build();
    }
}