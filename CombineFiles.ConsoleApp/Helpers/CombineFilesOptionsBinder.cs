using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Parsing;
using CombineFiles.Core.Configuration;
using CombineFiles.Core.Services;

namespace CombineFiles.ConsoleApp.Helpers;

public class CombineFilesOptionsBinder : BinderBase<CombineFilesOptions>
{
    private readonly Option<bool> _helpOption;
    private readonly Option<bool> _listPresetsOption;
    private readonly Option<string> _presetOption;
    private readonly Option<string> _modeOption;
    private readonly Option<List<string>> _extensionsOption;
    private readonly Option<List<string>> _excludePathsOption;
    private readonly Option<List<string>> _excludeFilePatternsOption;
    private readonly Option<string> _outputFileOption;
    private readonly Option<bool> _recurseOption;
    private readonly Option<bool> _enableLogOption;
    private readonly Option<bool> _interactiveOption;
    private readonly Option<int> _maxTokensOption;
    private readonly Option<string> _partialFileModeOption;
    private readonly Option<bool> _debugOption;

    public CombineFilesOptionsBinder(Option<bool> helpOption,
        Option<bool> listPresetsOption,
        Option<string> presetOption,
        Option<string> modeOption,
        Option<List<string>> extensionsOption,
        Option<List<string>> excludePathsOption,
        Option<List<string>> excludeFilePatternsOption,
        Option<string> outputFileOption,
        Option<bool> recurseOption,
        Option<bool> enableLogOption, 
        Option<bool> interactiveOption,
        Option<int> maxTokensOption,
        Option<string> partialFileModeOption,
        Option<bool> debugOption)
    {
        _helpOption = helpOption;
        _listPresetsOption = listPresetsOption;
        _presetOption = presetOption;
        _modeOption = modeOption;
        _extensionsOption = extensionsOption;
        _excludePathsOption = excludePathsOption;
        _excludeFilePatternsOption = excludeFilePatternsOption;
        _outputFileOption = outputFileOption;
        _recurseOption = recurseOption;
        _enableLogOption = enableLogOption;
        _interactiveOption = interactiveOption;
        _maxTokensOption = maxTokensOption;
        _partialFileModeOption = partialFileModeOption;
        _debugOption = debugOption;
    }

    protected override CombineFilesOptions GetBoundValue(BindingContext bindingContext)
    {
        // Conversione della stringa PartialFileMode in enum TokenLimitStrategy
        string partialFileModeStr = bindingContext.ParseResult.GetValueForOption(_partialFileModeOption) ?? "exclude";
        TokenLimitStrategy partialFileModeEnum = partialFileModeStr.Trim().ToLowerInvariant() == "partial"
            ? TokenLimitStrategy.IncludePartial
            : TokenLimitStrategy.ExcludeCompletely;

        return new CombineFilesOptions
        {
            Help = bindingContext.ParseResult.GetValueForOption(_helpOption),
            ListPresets = bindingContext.ParseResult.GetValueForOption(_listPresetsOption),
            Preset = bindingContext.ParseResult.GetValueForOption(_presetOption),
            Mode = bindingContext.ParseResult.GetValueForOption(_modeOption),
            Extensions = bindingContext.ParseResult.GetValueForOption(_extensionsOption) ?? new List<string>(),
            ExcludePaths = bindingContext.ParseResult.GetValueForOption(_excludePathsOption) ?? new List<string>(),
            ExcludeFilePatterns = bindingContext.ParseResult.GetValueForOption(_excludeFilePatternsOption) ?? new List<string>(),
            OutputFile = bindingContext.ParseResult.GetValueForOption(_outputFileOption),
            Recurse = bindingContext.ParseResult.GetValueForOption(_recurseOption),
            EnableLog = bindingContext.ParseResult.GetValueForOption(_enableLogOption),
            Interactive = bindingContext.ParseResult.GetValueForOption(_interactiveOption),
            MaxTotalTokens = bindingContext.ParseResult.GetValueForOption(_maxTokensOption),
            PartialFileMode = partialFileModeEnum,
            Debug = bindingContext.ParseResult.GetValueForOption(_debugOption)
        };
    }
}