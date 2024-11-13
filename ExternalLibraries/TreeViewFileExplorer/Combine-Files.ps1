param (
    [Parameter(Mandatory = $true, HelpMessage = "La modalità di selezione dei file: 'list', 'extensions', 'regex'.")]
    [ValidateSet("list", "extensions", "regex")]
    [string]$Mode,

    [Parameter(Mandatory = $false, HelpMessage = "Array di nomi di file da combinare. Utilizzato quando la modalità è 'list'.")]
    [string[]]$FileList,

    [Parameter(Mandatory = $false, HelpMessage = "Array di estensioni di file da includere (es. '.xaml', '.cs'). Utilizzato quando la modalità è 'extensions'.")]
    [string[]]$Extensions,

    [Parameter(Mandatory = $false, HelpMessage = "Array di pattern regex per selezionare i file. Utilizzato quando la modalità è 'regex'.")]
    [string[]]$RegexPatterns,

    [Parameter(Mandatory = $false, HelpMessage = "Percorso del file di output. Default: 'CombinedFile.txt' nella cartella corrente.")]
    [string]$OutputFile = "CombinedFile.txt",

    [Parameter(Mandatory = $false, HelpMessage = "Indica se cercare anche nelle sottocartelle.")]
    [switch]$Recurse,

    [Parameter(Mandatory = $false, HelpMessage = "Indica se stampare solo i nomi dei file invece che anche il contenuto.")]
    [switch]$FileNamesOnly,

    [Parameter(Mandatory = $false, HelpMessage = "Indica se stampare a video invece che creare un file.")]
    [switch]$OutputToConsole,

    [Parameter(Mandatory = $false, HelpMessage = "Array di percorsi di file o cartelle da escludere.")]
    [string[]]$ExcludePaths
)

function Combine-Files {
    <#
    .SYNOPSIS
    Combina il contenuto di più file o stampa i nomi dei file in base alle opzioni.

    .DESCRIPTION
    Questa funzione permette di combinare file o stampare i loro nomi, basandosi su una lista specifica, estensioni o espressioni regolari.
    Può operare nella cartella corrente e, opzionalmente, nelle sottocartelle.
    Supporta l'esclusione di file o cartelle specifici.

    .PARAMETER Mode
    La modalità di selezione dei file. Valori possibili: 'list', 'extensions', 'regex'.

    .PARAMETER FileList
    (Opzionale) Array di nomi di file da combinare. Utilizzato quando la modalità è 'list'.

    .PARAMETER Extensions
    (Opzionale) Array di estensioni di file da includere (es. '.xaml', '.cs'). Utilizzato quando la modalità è 'extensions'.

    .PARAMETER RegexPatterns
    (Opzionale) Array di pattern regex per selezionare i file. Utilizzato quando la modalità è 'regex'.

    .PARAMETER OutputFile
    (Opzionale) Percorso del file di output. Default: 'CombinedFile.txt' nella cartella corrente.

    .PARAMETER Recurse
    (Opzionale) Indica se cercare anche nelle sottocartelle. Default: $false.

    .PARAMETER FileNamesOnly
    (Opzionale) Indica se stampare solo i nomi dei file invece che anche il contenuto.

    .PARAMETER OutputToConsole
    (Opzionale) Indica se stampare a video invece che creare un file.

    .PARAMETER ExcludePaths
    (Opzionale) Array di percorsi di file o cartelle da escludere.

    .EXAMPLE
    .\Combine-Files.ps1 -Mode 'extensions' -Extensions '.cs' -OutputFile 'CombinedCSFiles.txt' -Recurse

    Combina tutti i file con estensione .cs nella cartella corrente e nelle sottocartelle, salvando il risultato in 'CombinedCSFiles.txt'.

    .EXAMPLE
    .\Combine-Files.ps1 -Mode 'list' -FileList 'file1.txt', 'file2.txt' -FileNamesOnly -OutputToConsole

    Stampa a video i nomi di 'file1.txt' e 'file2.txt' nella cartella corrente.
    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateSet("list", "extensions", "regex")]
        [string]$Mode,

        [Parameter(Mandatory = $false)]
        [string[]]$FileList,

        [Parameter(Mandatory = $false)]
        [string[]]$Extensions,

        [Parameter(Mandatory = $false)]
        [string[]]$RegexPatterns,

        [Parameter(Mandatory = $false)]
        [string]$OutputFile = "CombinedFile.txt",

        [Parameter(Mandatory = $false)]
        [switch]$Recurse,

        [Parameter(Mandatory = $false)]
        [switch]$FileNamesOnly,

        [Parameter(Mandatory = $false)]
        [switch]$OutputToConsole,

        [Parameter(Mandatory = $false)]
        [string[]]$ExcludePaths
    )

    # Ottieni il percorso corrente
    $sourcePath = Get-Location
    Write-Output "Percorso sorgente: $sourcePath"

    # Converti il percorso relativo del file di output in un percorso assoluto
    if (-not [System.IO.Path]::IsPathRooted($OutputFile)) {
        $OutputFile = Join-Path -Path $sourcePath -ChildPath $OutputFile
    }
    Write-Output "File di output: $OutputFile"

    # Normalizza i percorsi in $ExcludePaths a percorsi completi
    $fullExcludePaths = @()
    if ($ExcludePaths) {
        foreach ($path in $ExcludePaths) {
            if (-not [System.IO.Path]::IsPathRooted($path)) {
                $fullPath = Join-Path -Path $sourcePath -ChildPath $path
            } else {
                $fullPath = $path
            }
            $resolvedPaths = Resolve-Path -Path $fullPath -ErrorAction SilentlyContinue
            if ($resolvedPaths) {
                $fullExcludePaths += $resolvedPaths.Path
            }
        }
        Write-Output "Percorsi esclusi: $($fullExcludePaths -join ', ')"
    }

    # Inizializza la lista dei file da processare
    $filesToProcess = @()

    switch ($Mode) {
        'list' {
            if (-not $FileList) {
                Write-Error "La modalità 'list' richiede il parametro -FileList."
                return
            }

            Write-Output "Modalità selezione: Lista specifica di file."

            foreach ($file in $FileList) {
                $filePath = Join-Path -Path $sourcePath -ChildPath $file
                $fullFilePath = Resolve-Path -Path $filePath -ErrorAction SilentlyContinue
                if ($fullFilePath -and (Test-Path $fullFilePath -PathType Leaf)) {
                    if (-not ($fullExcludePaths -contains $fullFilePath.Path)) {
                        $filesToProcess += $fullFilePath.Path
                        Write-Output "File aggiunto dalla lista: $($fullFilePath.Path)"
                    }
                    else {
                        Write-Output "File escluso: $($fullFilePath.Path)"
                    }
                }
                else {
                    Write-Warning "File non trovato: $filePath"
                }
            }
        }
        'extensions' {
            if (-not $Extensions) {
                Write-Error "La modalità 'extensions' richiede il parametro -Extensions."
                return
            }

            Write-Output "Modalità selezione: Estensioni di file."

            foreach ($ext in $Extensions) {
                # Assicurati che l'estensione inizi con un punto
                $ext = if ($ext.StartsWith('.')) { $ext } else { ".$ext" }
                Write-Output "Ricerca per estensione: $ext"
                $matchedFiles = Get-ChildItem -Path $sourcePath -File -Filter "*$ext" -Recurse:$Recurse -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
                $filesToProcess += $matchedFiles
                Write-Output "$($matchedFiles.Count) file trovati con estensione $ext."
            }

            # Rimuovi duplicati
            $filesToProcess = $filesToProcess | Sort-Object -Unique
            Write-Output "Totale file da processare dopo rimozione duplicati: $($filesToProcess.Count)"
        }
        'regex' {
            if (-not $RegexPatterns) {
                Write-Error "La modalità 'regex' richiede il parametro -RegexPatterns."
                return
            }

            Write-Output "Modalità selezione: Espressioni regolari."

            $allFiles = Get-ChildItem -Path $sourcePath -File -Recurse:$Recurse -ErrorAction SilentlyContinue

            foreach ($file in $allFiles) {
                foreach ($pattern in $RegexPatterns) {
                    if ($file.Name -match $pattern) {
                        $filesToProcess += $file.FullName
                        Write-Output "File corrispondente al pattern '$pattern': $($file.FullName)"
                        break  # Evita di aggiungere lo stesso file più volte se corrisponde a più pattern
                    }
                }
            }

            # Rimuovi duplicati
            $filesToProcess = $filesToProcess | Sort-Object -Unique
            Write-Output "Totale file da processare dopo rimozione duplicati: $($filesToProcess.Count)"
        }
    }

    # Filtra i file da processare per escludere quelli in $ExcludePaths
    if ($fullExcludePaths) {
        $filesToProcess = $filesToProcess | Where-Object {
            $filePath = $_
            $isExcluded = $false
            foreach ($excludePath in $fullExcludePaths) {
                if (Test-Path $excludePath -PathType Container) {
                    if ($filePath -like "$excludePath*") {
                        $isExcluded = $true
                        break
                    }
                } else {
                    if ($filePath -eq $excludePath) {
                        $isExcluded = $true
                        break
                    }
                }
            }
            -not $isExcluded
        }
        Write-Output "Totale file da processare dopo esclusione: $($filesToProcess.Count)"
    }

    # Verifica se sono stati trovati file da unire
    if ($filesToProcess.Count -eq 0) {
        Write-Warning "Nessun file trovato per l'unione."
        return
    }
    else {
        Write-Output "Trovati $($filesToProcess.Count) file da processare."
    }

    # Se non si stampa a video, crea o svuota il file di output
    if (-not $OutputToConsole) {
        try {
            Out-File -FilePath $OutputFile -Force -Encoding UTF8
            Write-Output "File di output creato/svuotato: $OutputFile"
        }
        catch {
            Write-Error "Impossibile creare o scrivere nel file di output: $OutputFile"
            return
        }
    }

    # Processa ogni file
    foreach ($filePath in $filesToProcess) {
        $fileName = [System.IO.Path]::GetFileName($filePath)
        if ($FileNamesOnly) {
            $outputContent = "### $fileName ###"
        } else {
            $outputContent = "### Contenuto di $fileName ###"
        }

        if ($OutputToConsole) {
            Write-Output $outputContent
        } else {
            Add-Content -Path $OutputFile -Value $outputContent
        }

        if (-not $FileNamesOnly) {
            Write-Output "Aggiungendo contenuto di: $fileName"
            try {
                $fileContent = Get-Content -Path $filePath -ErrorAction Stop
                if ($OutputToConsole) {
                    $fileContent | Write-Output
                    Write-Output ""  # Linea vuota per separare i file
                } else {
                    $fileContent | Add-Content -Path $OutputFile
                    Add-Content -Path $OutputFile -Value "`n"  # Aggiunge una linea vuota per separare i file
                }
                Write-Output "File aggiunto correttamente: $fileName"
            }
            catch {
                Write-Warning "Impossibile leggere il file: $filePath"
            }
        }
    }

    if (-not $OutputToConsole) {
        Write-Output "Operazione completata. Controlla il file '$OutputFile'."
    }
}

# Chiama la funzione con i parametri passati allo script
Combine-Files -Mode $Mode -FileList $FileList -Extensions $Extensions -RegexPatterns $RegexPatterns -OutputFile $OutputFile -Recurse:$Recurse -FileNamesOnly:$FileNamesOnly -OutputToConsole:$OutputToConsole -ExcludePaths $ExcludePaths
