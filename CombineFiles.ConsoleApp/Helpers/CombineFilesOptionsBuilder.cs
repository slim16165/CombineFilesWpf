using System.Collections.Generic;
using CombineFiles.Core.Configuration;
using CombineFiles.Core.Services;

namespace CombineFiles.ConsoleApp.Helpers;

/// <summary>
/// Builder pattern per creare CombineFilesOptions in modo fluente.
/// Elimina la necessità di passare 15 parametri al metodo CreateOptions.
/// </summary>
public class CombineFilesOptionsBuilder
{
    private bool _help;
    private bool _listPresets;
    private string? _preset;
    private string? _mode;
    private List<string>? _extensions;
    private List<string>? _excludePaths;
    private List<string>? _excludeFilePatterns;
    private string? _outputFile;
    private bool _recurse;
    private bool _enableLog;
    private bool _interactive;
    private int _maxTokens;
    private string? _partialFileMode;
    private bool _compactSpaces;
    private bool _compactLLM;

    /// <summary>
    /// Crea un nuovo builder con valori di default.
    /// </summary>
    public CombineFilesOptionsBuilder()
    {
        // Valori di default
        _extensions = new List<string>();
        _excludePaths = new List<string>();
        _excludeFilePatterns = new List<string>();
    }

    /// <summary>
    /// Imposta il flag di help.
    /// </summary>
    public CombineFilesOptionsBuilder WithHelp(bool help)
    {
        _help = help;
        return this;
    }

    /// <summary>
    /// Imposta il flag per listare i preset.
    /// </summary>
    public CombineFilesOptionsBuilder WithListPresets(bool listPresets)
    {
        _listPresets = listPresets;
        return this;
    }

    /// <summary>
    /// Imposta il preset da utilizzare.
    /// </summary>
    public CombineFilesOptionsBuilder WithPreset(string? preset)
    {
        _preset = preset;
        return this;
    }

    /// <summary>
    /// Imposta la modalità di esecuzione.
    /// </summary>
    public CombineFilesOptionsBuilder WithMode(string? mode)
    {
        _mode = mode;
        return this;
    }

    /// <summary>
    /// Imposta le estensioni dei file da includere.
    /// </summary>
    public CombineFilesOptionsBuilder WithExtensions(List<string>? extensions)
    {
        _extensions = extensions ?? new List<string>();
        return this;
    }

    /// <summary>
    /// Imposta i percorsi da escludere.
    /// </summary>
    public CombineFilesOptionsBuilder WithExcludePaths(List<string>? excludePaths)
    {
        _excludePaths = excludePaths ?? new List<string>();
        return this;
    }

    /// <summary>
    /// Imposta i pattern di file da escludere.
    /// </summary>
    public CombineFilesOptionsBuilder WithExcludeFilePatterns(List<string>? excludeFilePatterns)
    {
        _excludeFilePatterns = excludeFilePatterns ?? new List<string>();
        return this;
    }

    /// <summary>
    /// Imposta il file di output.
    /// </summary>
    public CombineFilesOptionsBuilder WithOutputFile(string? outputFile)
    {
        _outputFile = outputFile;
        return this;
    }

    /// <summary>
    /// Imposta il flag di ricorsione.
    /// </summary>
    public CombineFilesOptionsBuilder WithRecurse(bool recurse)
    {
        _recurse = recurse;
        return this;
    }

    /// <summary>
    /// Imposta il flag per abilitare il logging.
    /// </summary>
    public CombineFilesOptionsBuilder WithEnableLog(bool enableLog)
    {
        _enableLog = enableLog;
        return this;
    }

    /// <summary>
    /// Imposta il flag per la modalità interattiva.
    /// </summary>
    public CombineFilesOptionsBuilder WithInteractive(bool interactive)
    {
        _interactive = interactive;
        return this;
    }

    /// <summary>
    /// Imposta il numero massimo di token.
    /// </summary>
    public CombineFilesOptionsBuilder WithMaxTokens(int maxTokens)
    {
        _maxTokens = maxTokens;
        return this;
    }

    /// <summary>
    /// Imposta la modalità per i file parziali (come stringa).
    /// </summary>
    public CombineFilesOptionsBuilder WithPartialFileMode(string? partialFileMode)
    {
        _partialFileMode = partialFileMode;
        return this;
    }

    /// <summary>
    /// Imposta il flag per compattare spazi in tab.
    /// </summary>
    public CombineFilesOptionsBuilder WithCompactSpaces(bool compactSpaces)
    {
        _compactSpaces = compactSpaces;
        return this;
    }

    /// <summary>
    /// Imposta il flag per compattare per LLM.
    /// </summary>
    public CombineFilesOptionsBuilder WithCompactLLM(bool compactLLM)
    {
        _compactLLM = compactLLM;
        return this;
    }

    /// <summary>
    /// Costruisce l'istanza di CombineFilesOptions con i valori impostati.
    /// </summary>
    public CombineFilesOptions Build()
    {
        // Conversione della stringa PartialFileMode in enum TokenLimitStrategy
        string partialFileModeStr = _partialFileMode ?? "partial";
        TokenLimitStrategy partialFileModeEnum = partialFileModeStr.Trim().ToLowerInvariant() switch
        {
            "partial" => TokenLimitStrategy.IncludePartial,
            "exclude" => TokenLimitStrategy.ExcludeCompletely,
            "paginate" => TokenLimitStrategy.PaginateOutput,
            _ => TokenLimitStrategy.IncludePartial
        };

        return new CombineFilesOptions
        {
            Help = _help,
            ListPresets = _listPresets,
            Preset = _preset,
            Mode = _mode,
            Extensions = _extensions ?? new List<string>(),
            ExcludePaths = _excludePaths ?? new List<string>(),
            ExcludeFilePatterns = _excludeFilePatterns ?? new List<string>(),
            OutputFile = _outputFile,
            Recurse = _recurse,
            EnableLog = _enableLog,
            Interactive = _interactive,
            MaxTotalTokens = _maxTokens,
            PartialFileMode = partialFileModeEnum,
            CompactSpacesToTabs = _compactSpaces,
            CompactForLLM = _compactLLM,
        };
    }
}
