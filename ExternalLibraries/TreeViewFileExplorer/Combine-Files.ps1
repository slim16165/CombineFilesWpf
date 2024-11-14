# Combine-Files.ps1
<#
.SYNOPSIS
Combina il contenuto di più file o stampa i nomi dei file in base alle opzioni, con supporto per preset predefiniti.

.DESCRIPTION
Questa funzione permette di combinare file o stampare i loro nomi, basandosi su una lista specifica, estensioni o espressioni regolari.
Supporta l'uso di preset predefiniti per configurazioni comuni, come il preset 'CSharp', e permette di estendere o sovrascrivere tali preset con parametri aggiuntivi.
Include miglioramenti nella gestione delle esclusioni e un'interfaccia utente migliorata per una migliore esperienza d'uso.

.PARAMETER Preset
Nome del preset da utilizzare. Esempi: 'CSharp'.

.PARAMETER ListPresets
Elenca i preset disponibili.

.PARAMETER Mode
La modalità di selezione dei file: 'list', 'extensions', 'regex'.

.PARAMETER FileList
(Array) Nomi di file da combinare. Utilizzato quando la modalità è 'list'.

.PARAMETER Extensions
(Array) Estensioni di file da includere (es. '.xaml', '.cs'). Utilizzato quando la modalità è 'extensions'.

.PARAMETER RegexPatterns
(Array) Pattern regex per selezionare i file. Utilizzato quando la modalità è 'regex'.

.PARAMETER OutputFile
Percorso del file di output. Default: 'CombinedFile.txt' nella cartella corrente.

.PARAMETER Recurse
Indica se cercare anche nelle sottocartelle.

.PARAMETER FileNamesOnly
Indica se stampare solo i nomi dei file invece che anche il contenuto.

.PARAMETER OutputToConsole
Indica se stampare a video invece che creare un file.

.PARAMETER ExcludePaths
(Array) Percorsi di file o cartelle da escludere.

.PARAMETER OutputEncoding
Encoding del file di output. Default: UTF8. Valori possibili: 'UTF8', 'ASCII', 'UTF7', 'UTF32', 'Unicode', 'Default'.

.PARAMETER OutputFormat
Formato del file di output: 'txt', 'csv', 'json'. Default: 'txt'.

.PARAMETER MinDate
Data minima per i file da includere.

.PARAMETER MaxDate
Data massima per i file da includere.

.PARAMETER MinSize
Dimensione minima dei file (es. '1MB').

.PARAMETER MaxSize
Dimensione massima dei file (es. '10MB').

.EXAMPLE
.\Combine-Files.ps1 -Preset 'CSharp'

Combina tutti i file con estensione .cs e .xaml nelle cartelle correnti e sottocartelle, escludendo 'Properties', 'obj', 'bin', salvando in 'CombinedFile.cs'.

.EXAMPLE
.\Combine-Files.ps1 -Preset 'CSharp' -ExcludePaths 'AdditionalFolder'

Combina i file secondo il preset 'CSharp' ed esclude anche 'AdditionalFolder'.

.EXAMPLE
.\Combine-Files.ps1 -ListPresets

Elenca tutti i preset disponibili.

#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false, HelpMessage = "Nome del preset da utilizzare.")]
    [ValidateNotNullOrEmpty()]
    [string]$Preset,

    [Parameter(Mandatory = $false, HelpMessage = "Elenca i preset disponibili.")]
    [switch]$ListPresets,

    [Parameter(Mandatory = $false, HelpMessage = "La modalità di selezione dei file: 'list', 'extensions', 'regex'.")]
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
    [string[]]$ExcludePaths,

    [Parameter(Mandatory = $false, HelpMessage = "Encoding del file di output. Default: UTF8.")]
    [ValidateSet("UTF8", "ASCII", "UTF7", "UTF32", "Unicode", "Default")]
    [string]$OutputEncoding = "UTF8",

    [Parameter(Mandatory = $false, HelpMessage = "Formato del file di output: 'txt', 'csv', 'json'. Default: 'txt'.")]
    [ValidateSet("txt", "csv", "json")]
    [string]$OutputFormat = "txt",

    [Parameter(Mandatory = $false, HelpMessage = "Data minima per i file da includere.")]
    [datetime]$MinDate,

    [Parameter(Mandatory = $false, HelpMessage = "Data massima per i file da includere.")]
    [datetime]$MaxDate,

    [Parameter(Mandatory = $false, HelpMessage = "Dimensione minima dei file (es. '1MB').")]
    [string]$MinSize,

    [Parameter(Mandatory = $false, HelpMessage = "Dimensione massima dei file (es. '10MB').")]
    [string]$MaxSize
)

# Definizione dei preset
$Presets = @{
    "CSharp" = @{
        Mode = 'extensions'
        Extensions = '.cs', '.xaml'
        OutputFile = 'CombinedFile.cs'
        Recurse = $true
        ExcludePaths = 'Properties', 'obj', 'bin'
    }
    # Puoi aggiungere altri preset qui
}

# Funzioni ausiliarie

function Write-Log {
    param (
        [string]$Message,
        [string]$Level = "INFO"
    )
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "$timestamp [$Level] $Message"
    Add-Content -Path $logFile -Value $logMessage
}

function Convert-SizeToBytes {
    param ([string]$size)
    switch -Regex ($size) {
        '^(\d+)\s*KB$' { return [int64]$matches[1] * 1KB }
        '^(\d+)\s*MB$' { return [int64]$matches[1] * 1MB }
        '^(\d+)\s*GB$' { return [int64]$matches[1] * 1GB }
        '^(\d+)$'      { return [int64]$matches[1] }
        default {
            Write-Log "Formato di dimensione non riconosciuto: $size" "ERROR"
            throw "Formato di dimensione non riconosciuto: $size"
        }
    }
}

function Get-FilesToProcess {
    param (
        [string]$Mode,
        [string[]]$FileList,
        [string[]]$Extensions,
        [string[]]$RegexPatterns,
        [string]$sourcePath,
        [switch]$Recurse,
        [string[]]$fullExcludePaths
    )

    $files = @()
    switch ($Mode) {
        'list' {
            foreach ($file in $FileList) {
                $filePath = Join-Path -Path $sourcePath -ChildPath $file
                $resolved = Resolve-Path -Path $filePath -ErrorAction SilentlyContinue
                if ($resolved -and (Test-Path $resolved.Path -PathType Leaf)) {
                    if (-not (Is-PathExcluded $resolved.Path $fullExcludePaths)) {
                        $files += $resolved.Path
                        Write-Log "File aggiunto dalla lista: $($resolved.Path)"
                    }
                    else {
                        Write-Log "File escluso (lista): $($resolved.Path)"
                    }
                }
                else {
                    Write-Warning "File non trovato: $filePath"
                    Write-Log "File non trovato: $filePath" "WARNING"
                }
            }
        }
        'extensions' {
            foreach ($ext in $Extensions) {
                $ext = if ($ext.StartsWith('.')) { $ext } else { ".$ext" }
                Write-Log "Ricerca per estensione: $ext"
                $matched = Get-ChildItem -Path $sourcePath -File -Filter "*$ext" -Recurse:$Recurse -ErrorAction SilentlyContinue | Where-Object { -not (Is-PathExcluded $_.FullName $fullExcludePaths) } | Select-Object -ExpandProperty FullName
                $files += $matched
                Write-Log "$($matched.Count) file trovati con estensione $ext."
            }
            $files = $files | Sort-Object -Unique
            Write-Log "Totale file da processare dopo rimozione duplicati: $($files.Count)"
        }
        'regex' {
            $allFiles = Get-ChildItem -Path $sourcePath -File -Recurse:$Recurse -ErrorAction SilentlyContinue
            foreach ($file in $allFiles) {
                foreach ($pattern in $RegexPatterns) {
                    if ($file.Name -match $pattern) {
                        if (-not (Is-PathExcluded $file.FullName $fullExcludePaths)) {
                            $files += $file.FullName
                            Write-Log "File corrispondente al pattern '$pattern': $($file.FullName)"
                        }
                        break  # Evita di aggiungere lo stesso file più volte se corrisponde a più pattern
                    }
                }
            }
            $files = $files | Sort-Object -Unique
            Write-Log "Totale file da processare dopo rimozione duplicati: $($files.Count)"
        }
    }

    return $files
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

function Is-PathExcluded {
    param (
        [string]$FilePath,
        [string[]]$ExcludedPaths
    )
    foreach ($excluded in $ExcludedPaths) {
        if ($FilePath -ieq $excluded) {
            return $true
        }
        if ($FilePath.StartsWith($excluded + [IO.Path]::DirectorySeparatorChar, [System.StringComparison]::InvariantCultureIgnoreCase)) {
            return $true
        }
    }
    return $false
}

# Inizio dello script

# Definisci un percorso per il log
$sourcePath = Get-Location
$logFile = Join-Path -Path $sourcePath -ChildPath "CombineFiles.log"

# Inizializza il log
try {
    Out-File -FilePath $logFile -Force -Encoding UTF8
    Write-Log "Inizio operazione di combinazione file."
}
catch {
    Write-Error "Impossibile creare il file di log: $logFile"
    Write-Host "Impossibile creare il file di log: $logFile" -ForegroundColor Red
    return
}

# Funzione per visualizzare messaggi colorati
function Show-Message {
    param (
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Gestione del parametro -ListPresets
if ($ListPresets) {
    Show-Message "Preset disponibili:" "Cyan"
    foreach ($preset in $Presets.Keys) {
        Show-Message "- $preset" "Green"
    }
    Write-Log "Elenco dei preset richiesto dall'utente."
    return
}

# Applicazione dei preset
if ($Preset) {
    if ($Presets.ContainsKey($Preset)) {
        $presetParams = $Presets[$Preset]
        foreach ($key in $presetParams.Keys) {
            if (-not $PSBoundParameters.ContainsKey($key)) {
                if ($presetParams[$key] -is [System.Collections.IEnumerable] -and -not ($presetParams[$key] -is [string])) {
                    # Se il parametro è un array, unisci con eventuali valori già presenti
                    if ($key -eq 'ExcludePaths') {
                        $ExcludePaths += $presetParams[$key]
                    }
                    elseif ($key -eq 'Extensions') {
                        $Extensions += $presetParams[$key]
                    }
                    # Aggiungi altri array se necessario
                }
                else {
                    # Imposta il valore del parametro se non è un array
                    Set-Variable -Name $key -Value $presetParams[$key]
                }
                Write-Log "Applicato preset '$Preset': $key = $($presetParams[$key])"
                Show-Message "Applicato preset '$Preset': $key = $($presetParams[$key])" "Yellow"
            }
        }
    }
    else {
        Write-Error "Preset '$Preset' non trovato."
        Write-Log "Errore: Preset '$Preset' non trovato." "ERROR"
        Show-Message "Errore: Preset '$Preset' non trovato." "Red"
        return
    }
}

Write-Log "Percorso sorgente: $sourcePath"
Write-Log "File di output: $OutputFile"

# Converti il percorso relativo del file di output in un percorso assoluto
if (-not [System.IO.Path]::IsPathRooted($OutputFile)) {
    $OutputFile = Join-Path -Path $sourcePath -ChildPath $OutputFile
}
Write-Log "Percorso assoluto del file di output: $OutputFile"

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

# Validazione dei parametri

if ($Mode -eq 'list' -and -not $FileList) {
    Write-Error "La modalità 'list' richiede il parametro -FileList."
    Write-Log "Errore: Modalità 'list' senza -FileList." "ERROR"
    Show-Message "Errore: La modalità 'list' richiede il parametro -FileList." "Red"
    return
}

if ($Mode -eq 'extensions' -and -not $Extensions) {
    Write-Error "La modalità 'extensions' richiede il parametro -Extensions."
    Write-Log "Errore: Modalità 'extensions' senza -Extensions." "ERROR"
    Show-Message "Errore: La modalità 'extensions' richiede il parametro -Extensions." "Red"
    return
}

if ($Mode -eq 'regex' -and -not $RegexPatterns) {
    Write-Error "La modalità 'regex' richiede il parametro -RegexPatterns."
    Write-Log "Errore: Modalità 'regex' senza -RegexPatterns." "ERROR"
    Show-Message "Errore: La modalità 'regex' richiede il parametro -RegexPatterns." "Red"
    return
}

# Validazione delle estensioni
if ($Mode -eq 'extensions') {
    foreach ($ext in $Extensions) {
        if (-not $ext.StartsWith('.')) {
            Write-Error "L'estensione '$ext' deve iniziare con un punto."
            Write-Log "Errore: Estensione non valida '$ext'." "ERROR"
            Show-Message "Errore: L'estensione '$ext' deve iniziare con un punto." "Red"
            return
        }
    }
}

# Validazione dei percorsi di esclusione
if ($ExcludePaths) {
    foreach ($path in $ExcludePaths) {
        if (-not (Test-Path $path)) {
            Write-Warning "Percorso di esclusione non trovato: $path"
            Write-Log "Percorso di esclusione non trovato: $path" "WARNING"
            Show-Message "Attenzione: Percorso di esclusione non trovato: $path" "Magenta"
        }
    }
}

# Converti le dimensioni in byte
$minSizeBytes = if ($MinSize) { Convert-SizeToBytes $MinSize } else { 0 }
$maxSizeBytes = if ($MaxSize) { Convert-SizeToBytes $MaxSize } else { [int64]::MaxValue }

# Ottieni la lista dei file da processare
$filesToProcess = Get-FilesToProcess -Mode $Mode -FileList $FileList -Extensions $Extensions -RegexPatterns $RegexPatterns -sourcePath $sourcePath -Recurse:$Recurse -fullExcludePaths $fullExcludePaths

Write-Log "Numero iniziale di file da processare: $($filesToProcess.Count)"
Show-Message "Numero iniziale di file da processare: $($filesToProcess.Count)" "Cyan"

# Filtra i file basati su data e dimensione
if ($MinDate -or $MaxDate -or $MinSize -or $MaxSize) {
    $filesToProcess = $filesToProcess | Where-Object {
        $file = Get-Item $_
        $validDate = ($file.LastWriteTime -ge $MinDate -or -not $MinDate) -and
                     ($file.LastWriteTime -le $MaxDate -or -not $MaxDate)
        $validSize = ($file.Length -ge $minSizeBytes) -and
                     ($file.Length -le $maxSizeBytes)
        $validDate -and $validSize
    }
    Write-Log "Totale file dopo filtraggio per data e dimensione: $($filesToProcess.Count)"
    Show-Message "Totale file dopo filtraggio per data e dimensione: $($filesToProcess.Count)" "Cyan"
}

# Filtra nuovamente per escludere eventuali percorsi non gestiti
if ($fullExcludePaths) {
    $filesToProcess = $filesToProcess | Where-Object {
        -not (Is-PathExcluded $_ $fullExcludePaths)
    }
    Write-Log "Totale file da processare dopo esclusione finale: $($filesToProcess.Count)"
    Show-Message "Totale file da processare dopo esclusione finale: $($filesToProcess.Count)" "Cyan"
}

# Verifica se sono stati trovati file da unire
if ($filesToProcess.Count -eq 0) {
    Write-Warning "Nessun file trovato per l'unione."
    Write-Log "Nessun file trovato per l'unione." "WARNING"
    Show-Message "Nessun file trovato per l'unione." "Yellow"
    return
}
else {
    Write-Log "Trovati $($filesToProcess.Count) file da processare."
    Show-Message "Trovati $($filesToProcess.Count) file da processare." "Green"
}

# Configura l'encoding
$encodingSwitch = switch ($OutputEncoding) {
    'UTF8'    { "UTF8" }
    'ASCII'   { "ASCII" }
    'UTF7'    { "UTF7" }
    'UTF32'   { "UTF32" }
    'Unicode' { "Unicode" }
    'Default' { "Default" }
}

# Se non si stampa a video, crea o svuota il file di output
if (-not $OutputToConsole) {
    try {
        switch ($OutputFormat) {
            'txt' {
                Out-File -FilePath $OutputFile -Force -Encoding $encodingSwitch
            }
            'csv' {
                @() | Export-Csv -Path $OutputFile -Force -NoTypeInformation -Encoding $encodingSwitch
            }
            'json' {
                @() | ConvertTo-Json | Out-File -FilePath $OutputFile -Force -Encoding $encodingSwitch
            }
        }
        Write-Log "File di output creato/svuotato: $OutputFile"
        Show-Message "File di output creato/svuotato: $OutputFile" "Green"
    }
    catch {
        Write-Log "Impossibile creare o scrivere nel file di output: $OutputFile - $_" "ERROR"
        Show-Message "Errore: Impossibile creare o scrivere nel file di output: $OutputFile" "Red"
        return
    }
}

# Imposta la barra di avanzamento
$totalFiles = $filesToProcess.Count
$currentFile = 0

# Processa ogni file
foreach ($filePath in $filesToProcess) {
    $currentFile++
    Write-Progress -Activity "Combinazione dei file" -Status "Elaborazione file $currentFile di $totalFiles" -PercentComplete (($currentFile / $totalFiles) * 100)

    $fileName = [System.IO.Path]::GetFileName($filePath)
    $outputContent = if ($FileNamesOnly) { "### $fileName ###" } else { "### Contenuto di $fileName ###" }

    Write-OutputOrFile -Content $outputContent -OutputToConsole:$OutputToConsole -OutputFile $OutputFile -OutputFormat $OutputFormat

    if (-not $FileNamesOnly) {
        Write-Log "Aggiungendo contenuto di: $fileName"
        try {
            $fileContent = Get-Content -Path $filePath -ErrorAction Stop
            if ($OutputToConsole) {
                $fileContent | Write-Output
                Write-Output ""  # Linea vuota per separare i file
            }
            else {
                switch ($OutputFormat) {
                    'txt' {
                        Add-Content -Path $OutputFile -Value $fileContent
                        Add-Content -Path $OutputFile -Value "`n"  # Linea vuota
                    }
                    'csv' {
                        # Gestisci CSV come necessario
                        foreach ($line in $fileContent) {
                            Add-Content -Path $OutputFile -Value $line
                        }
                    }
                    'json' {
                        # Gestisci JSON come necessario
                        foreach ($line in $fileContent) {
                            Add-Content -Path $OutputFile -Value $line
                        }
                    }
                }
            }
            Write-Log "File aggiunto correttamente: $fileName"
        }
        catch {
            Write-Warning "Impossibile leggere il file: $filePath"
            Write-Log "Impossibile leggere il file: $filePath - $_" "WARNING"
            Show-Message "Attenzione: Impossibile leggere il file: $filePath" "Magenta"
        }
    }
}

# Messaggio di completamento
if (-not $OutputToConsole) {
    Write-Log "Operazione completata. Controlla il file '$OutputFile'."
    Show-Message "Operazione completata. Controlla il file '$OutputFile'." "Green"
}
else {
    Write-Log "Operazione completata con output a console."
    Show-Message "Operazione completata con output a console." "Green"
}