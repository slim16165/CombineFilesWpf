# InteractiveSelection.psm1

function Start-InteractiveSelection {
    param (
        [string[]]$InitialFiles,
        [string]$SourcePath
    )

    # Convertire i percorsi assoluti in relativi
    $relativePaths = $InitialFiles | ForEach-Object { 
        [System.IO.Path]::GetRelativePath($SourcePath, $_)
    }

    # Crea un file temporaneo
    $tempConfigFilePath = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "CombineFiles_InteractiveSelection.txt")

    # Scrivi l'elenco dei file nel file temporaneo
    $relativePaths | Out-File -FilePath $tempConfigFilePath -Encoding UTF8

    Write-Log "File di configurazione temporaneo creato: $tempConfigFilePath"
    Show-Message "File di configurazione temporaneo creato: $tempConfigFilePath" "Yellow"

    # Apri il file nell'editor predefinito
    $editor = $env:EDITOR
    if (-not $editor) {
        $editor = "notepad.exe"
    }

    Write-Log "Apertura del file di configurazione temporaneo con l'editor: $editor"
    Start-Process -FilePath $editor -ArgumentList $tempConfigFilePath -Wait

    Write-Log "Editor chiuso. Lettura del file di configurazione aggiornato."
    Show-Message "Editor chiuso. Lettura del file di configurazione aggiornato." "Green"

    # Leggi il file di configurazione aggiornato
    if (Test-Path $tempConfigFilePath) {
        $updatedRelativePaths = Get-Content -Path $tempConfigFilePath -ErrorAction Stop | Where-Object { $_.Trim() -ne "" }

        # Converti i percorsi relativi in assoluti
        $updatedFiles = $updatedRelativePaths | ForEach-Object {
            $absolutePath = Join-Path -Path $SourcePath -ChildPath $_
            if (Test-Path $absolutePath -PathType Leaf) {
                $absolutePath
            }
            else {
                Write-Warning "File non trovato durante la lettura del config: $absolutePath"
                Write-Log "File non trovato durante la lettura del config: $absolutePath" "WARNING"
                $null
            }
        } | Where-Object { $_ -ne $null }

        Write-Log "File aggiornati dopo InteractiveSelection: $($updatedFiles.Count)"
        Show-Message "File aggiornati dopo InteractiveSelection: $($updatedFiles.Count)" "Cyan"

        # Rimuovi il file temporaneo
        Remove-Item -Path $tempConfigFilePath -Force
        Write-Log "File di configurazione temporaneo rimosso: $tempConfigFilePath"
    }
    else {
        Write-Error "Il file di configurazione temporaneo non è stato trovato: $tempConfigFilePath"
        Write-Log "Errore: Il file di configurazione temporaneo non è stato trovato: $tempConfigFilePath" "ERROR"
        Show-Message "Errore: Il file di configurazione temporaneo non è stato trovato." "Red"
        return @()
    }

    return $updatedFiles
}
