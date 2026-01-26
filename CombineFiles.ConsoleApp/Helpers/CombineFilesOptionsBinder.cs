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
        bool compactLLM)
    {
        // Conversione della stringa PartialFileMode in enum TokenLimitStrategy
        string partialFileModeStr = partialFileMode ?? "partial";
        TokenLimitStrategy partialFileModeEnum = partialFileModeStr.Trim().ToLowerInvariant() switch
        {
            "partial" => TokenLimitStrategy.IncludePartial,
            "exclude" => TokenLimitStrategy.ExcludeCompletely,
            "paginate" => TokenLimitStrategy.PaginateOutput, // nuova modalità
            _ => TokenLimitStrategy.IncludePartial
        };

        return new CombineFilesOptions
        {
            Help = help,
            ListPresets = listPresets,
            Preset = preset,
            Mode = mode,
            Extensions = extensions ?? new List<string>(),
            ExcludePaths = excludePaths ?? new List<string>(),
            ExcludeFilePatterns = excludeFilePatterns ?? new List<string>(),
            OutputFile = outputFile,
            Recurse = recurse,
            EnableLog = enableLog,
            Interactive = interactive,
            MaxTotalTokens = maxTokens,
            PartialFileMode = partialFileModeEnum,
            CompactSpacesToTabs = compactSpaces,
            CompactForLLM = compactLLM,
        };
    }
}