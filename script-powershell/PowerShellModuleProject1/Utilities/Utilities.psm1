# Utilities.psm1

function Show-Message {
    param (
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-OutputOrFile {
    param (
        [string]$Content,
        [switch]$OutputToConsole,
        [string]$OutputFile,
        [string]$OutputFormat
    )

    if ($OutputToConsole) {
        Write-Output $Content
    } else {
        switch ($OutputFormat) {
            'txt' {
                Add-Content -Path $OutputFile -Value $Content
            }
            'csv' {
                # Per CSV, potrebbe essere necessario strutturare diversamente
                Add-Content -Path $OutputFile -Value $Content
            }
            'json' {
                # Per JSON, potrebbe essere necessario strutturare diversamente
                Add-Content -Path $OutputFile -Value $Content
            }
        }
    }
}
