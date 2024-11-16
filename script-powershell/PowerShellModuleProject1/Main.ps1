Import-Module -Name (Join-Path $PSScriptRoot 'Logging.psm1')
Import-Module -Name (Join-Path $PSScriptRoot 'FileSelection.psm1')
Import-Module -Name (Join-Path $PSScriptRoot 'InteractiveSelection.psm1')

function Start-CombineFiles {
    param (
        # Parametri principali dello script
    )
    # Logica principale
}

Start-CombineFiles @PSBoundParameters
