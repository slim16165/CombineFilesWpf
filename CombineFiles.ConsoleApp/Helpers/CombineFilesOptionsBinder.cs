using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using CombineFiles.Core.Configuration;

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

    public CombineFilesOptionsBinder(
        Option<bool> helpOption,
        Option<bool> listPresetsOption,
        Option<string> presetOption,
        Option<string> modeOption,
        Option<List<string>> extensionsOption,
        Option<List<string>> excludePathsOption,
        Option<List<string>> excludeFilePatternsOption,
        Option<string> outputFileOption,
        Option<bool> recurseOption,
        Option<bool> enableLogOption)
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
    }

    protected override CombineFilesOptions GetBoundValue(BindingContext bindingContext)
    {
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
            EnableLog = bindingContext.ParseResult.GetValueForOption(_enableLogOption)
        };
    }
}