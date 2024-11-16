# FileSelection.psm1

function Convert-SizeToBytes {
    param (
        [string]$Size
    )
    switch -Regex ($Size) {
        '^(\d+)\s*KB$' { return [int64]$matches[1] * 1KB }
        '^(\d+)\s*MB$' { return [int64]$matches[1] * 1MB }
        '^(\d+)\s*GB$' { return [int64]$matches[1] * 1GB }
        '^(\d+)$'      { return [int64]$matches[1] }
        default {
            Write-Log "Formato di dimensione non riconosciuto: $Size" "ERROR"
            throw "Formato di dimensione non riconosciuto: $Size"
        }
    }
}

function Is-PathExcluded {
    param (
        [string]$FilePath,
        [string[]]$ExcludedPaths,
        [string[]]$ExcludedFiles,
        [string[]]$ExcludedFilePatterns
    )
    # Verifica se il percorso del file è in una cartella esclusa
    foreach ($excluded in $ExcludedPaths) {
        if ($FilePath -ieq $excluded) {
            Write-Log "Escluso per percorso esatto: $FilePath corrisponde a $excluded"
            return $true
        }
        if ($FilePath.StartsWith($excluded + [IO.Path]::DirectorySeparatorChar, [System.StringComparison]::InvariantCultureIgnoreCase)) {
            Write-Log "Escluso per percorso: $FilePath inizia con $excluded"
            return $true
        }
    }

    # Verifica se il file è nella lista dei nomi esclusi
    foreach ($excludedFile in $ExcludedFiles) {
        if ([System.IO.Path]::GetFileName($FilePath) -ieq $excludedFile) {
            Write-Log "Escluso per nome file: $FilePath corrisponde a $excludedFile"
            return $true
        }
    }

    # Verifica se il file corrisponde a uno dei pattern esclusi
    foreach ($pattern in $ExcludedFilePatterns) {
        if ($FilePath -match $pattern) {
            Write-Log "Escluso per pattern regex: $FilePath corrisponde a $pattern"
            return $true
        }
    }

    Write-Log "File incluso: $FilePath"
    return $false
}

function Get-FilesToProcess {
    param (
        [string]$Mode,
        [string[]]$FileList,
        [string[]]$Extensions,
        [string[]]$RegexPatterns,
        [string]$SourcePath,
        [switch]$Recurse,
        [string[]]$FullExcludePaths,
        [string[]]$FullExcludeFiles,
        [string[]]$FullExcludeFilePatterns
    )

    $files = @()
    switch ($Mode) {
        'list' {
            foreach ($file in $FileList) {
                $filePath = Join-Path -Path $SourcePath -ChildPath $file
                $resolved = Resolve-Path -Path $filePath -ErrorAction SilentlyContinue
                if ($resolved -and (Test-Path $resolved.Path -PathType Leaf)) {
                    if (-not (Is-PathExcluded -FilePath $resolved.Path -ExcludedPaths $FullExcludePaths -ExcludedFiles $FullExcludeFiles -ExcludedFilePatterns $FullExcludeFilePatterns)) {
                        $files += $resolved.Path
                        Write-Log "File incluso dalla lista: $($resolved.Path)"
                    }
                    else {
                        Write-Log "File escluso dalla lista: $($resolved.Path)"
                    }
                }
                else {
                    Write-Log "File non trovato: $filePath" "WARNING"
                }
            }
        }
        'extensions' {
            foreach ($ext in $Extensions) {
                $ext = if ($ext.StartsWith('.')) { $ext } else { ".$ext" }
                Write-Log "Ricerca per estensione: $ext"
                $matched = Get-ChildItem -Path $SourcePath -File -Filter "*$ext" -Recurse:$Recurse -ErrorAction SilentlyContinue
                foreach ($file in $matched) {
                    if (-not (Is-PathExcluded -FilePath $file.FullName -ExcludedPaths $FullExcludePaths -ExcludedFiles $FullExcludeFiles -ExcludedFilePatterns $FullExcludeFilePatterns)) {
                        $files += $file.FullName
                        Write-Log "File incluso: $($file.FullName)"
                    }
                    else {
                        Write-Log "File escluso: $($file.FullName)"
                    }
                }
            }
            $files = $files | Sort-Object -Unique
            Write-Log "Totale file da processare dopo rimozione duplicati: $($files.Count)"
        }
        'regex' {
            $allFiles = Get-ChildItem -Path $SourcePath -File -Recurse:$Recurse -ErrorAction SilentlyContinue
            foreach ($file in $allFiles) {
                foreach ($pattern in $RegexPatterns) {
                    if ($file.Name -match $pattern) {
                        if (-not (Is-PathExcluded -FilePath $file.FullName -ExcludedPaths $FullExcludePaths -ExcludedFiles $FullExcludeFiles -ExcludedFilePatterns $FullExcludeFilePatterns)) {
                            $files += $file.FullName
                            Write-Log "File incluso per pattern regex: $($file.FullName)"
                        }
                        else {
                            Write-Log "File escluso per pattern regex: $($file.FullName)"
                        }
                        break  # Evita di aggiungere lo stesso file più volte se corrisponde a più pattern
                    }
                }
            }
            $files = $files | Sort-Object -Unique
            Write-Log "Totale file da processare dopo rimozione duplicati: $($files.Count)"
        }
        'InteractiveSelection' {
            # La modalità InteractiveSelection verrà gestita separatamente nel flusso principale
            Write-Log "Modalità 'InteractiveSelection' selezionata. Gestita nel flusso principale."
        }
    }

    return $files
}

function Normalize-ExcludePaths {
    param (
        [string[]]$ExcludePaths,
        [string]$SourcePath
    )

    $fullExcludePaths = @()
    if ($ExcludePaths) {
        foreach ($path in $ExcludePaths) {
            if (-not [System.IO.Path]::IsPathRooted($path)) {
                $fullPath = Join-Path -Path $SourcePath -ChildPath $path
            } else {
                $fullPath = $path
            }
            $resolvedPaths = Resolve-Path -Path $fullPath -ErrorAction SilentlyContinue
            if ($resolvedPaths) {
                foreach ($resolved in $resolvedPaths) {
                    $fullExcludePaths += $resolved.Path
                    Write-Log "Percorso escluso aggiunto: $($resolved.Path)"
                }
            }
            else {
                Write-Warning "Percorso di esclusione non trovato: $fullPath"
                Write-Log "Percorso di esclusione non trovato: $fullPath" "WARNING"
                Show-Message "Attenzione: Percorso di esclusione non trovato: $fullPath" "Magenta"
            }
        }
        Write-Log "Totale percorsi esclusi: $($fullExcludePaths.Count)"
    }
    return $fullExcludePaths
}

function Validate-Extensions {
    param (
        [string]$Mode,
        [string[]]$Extensions
    )
    if (($Mode -eq 'extensions' -or $Mode -eq 'InteractiveSelection') -and -not $Extensions) {
        Write-Error "La modalità '$Mode' richiede il parametro -Extensions."
        Write-Log "Errore: Modalità '$Mode' senza -Extensions." "ERROR"
        Show-Message "Errore: La modalità '$Mode' richiede il parametro -Extensions." "Red"
        throw "Errore: La modalità '$Mode' richiede il parametro -Extensions."
    }

    if ($Mode -eq 'extensions' -or $Mode -eq 'InteractiveSelection') {
        foreach ($ext in $Extensions) {
            if (-not $ext.StartsWith('.')) {
                Write-Error "L'estensione '$ext' deve iniziare con un punto."
                Write-Log "Errore: Estensione non valida '$ext'." "ERROR"
                Show-Message "Errore: L'estensione '$ext' deve iniziare con un punto." "Red"
                throw "Errore: L'estensione '$ext' deve iniziare con un punto."
            }
        }
    }
}
