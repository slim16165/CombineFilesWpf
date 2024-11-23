function Get-Preset {
    param (
        [string]$PresetName
    )

    $Presets = @{
        "CSharp" = @{
            Mode = 'extensions'
            Extensions = '.cs', '.xaml'
            OutputFile = 'CombinedFile.cs'
            Recurse = $true
            ExcludePaths = 'Properties', 'obj', 'bin'
            ExcludeFilePatterns = '.*\.g\.i\.cs$', '.*\.g\.cs$', '.*\.designer\.cs$', '.*AssemblyInfo\.cs$', '^auto-generated'
        }
        # Aggiungi altri preset qui, se necessario
    }

    if ($Presets.ContainsKey($PresetName)) {
        return $Presets[$PresetName]
    }
    else {
        throw "Preset '$PresetName' non trovato."
    }
}
