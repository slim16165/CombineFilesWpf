<#
.SYNOPSIS
Combina il contenuto di più file o stampa i nomi dei file in base alle opzioni, con supporto per preset predefiniti e modalità interattive.

.DESCRIPTION
Questa funzione permette di combinare file o stampare i loro nomi, basandosi su una lista specifica, estensioni o espressioni regolari.
Supporta l'uso di preset predefiniti per configurazioni comuni, come il preset 'CSharp', e permette di estendere o sovrascrivere tali preset con parametri aggiuntivi.
Include miglioramenti nella gestione delle esclusioni, una modalità interattiva per gestire manualmente l'elenco dei file e un'interfaccia utente migliorata per una migliore esperienza d'uso.
Supporta il tracciamento degli hard link per evitare la duplicazione dei file.
Supporta la navigazione di directory tramite hard link/junctions/symbolic links.

.PARAMETER Preset
Nome del preset da utilizzare. Esempi: 'CSharp'.

.PARAMETER ListPresets
Elenca i preset disponibili.

.PARAMETER Mode
La modalità di selezione dei file: 'list', 'extensions', 'regex', 'InteractiveSelection'.

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
(Array) Percorsi di cartelle da escludere.

.PARAMETER ExcludeFiles
(Array) Nomi di file da escludere.

.PARAMETER ExcludeFilePatterns
(Array) Pattern regex per escludere i file.

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

.PARAMETER EnableLog
Abilita la generazione del file di log.

.PARAMETER Help
Mostra questo messaggio di aiuto.

.EXAMPLE
.\Combine-Files.ps1 -Preset 'CSharp'

Combina tutti i file con estensione .cs e .xaml nelle cartelle correnti e sottocartelle, escludendo 'Properties', 'obj', 'bin', salvando in 'CombinedFile.cs'.

.EXAMPLE
.\Combine-Files.ps1 -EnableLog /help

Mostra il messaggio di aiuto.

.EXAMPLE
.\Combine-Files.ps1 -Mode 'extensions' -Extensions '.cs', '.xaml' -Recurse -EnableLog

Combina tutti i file con estensione .cs e .xaml nelle cartelle correnti e sottocartelle, seguendo le giunzioni/hard link.

#>

# Gestione del parametro /help prima di CmdletBinding
param (
    [Parameter(Mandatory=$false, Position=0)]
    [string[]]$Args
)

foreach ($arg in $Args) {
    if ($arg -ieq "/help") {
        Get-Help -Full
        exit
    }
}

[CmdletBinding(DefaultParameterSetName="Default")]
param (
    [Parameter(Mandatory = $false, HelpMessage = "Nome del preset da utilizzare.")]
    [ValidateNotNullOrEmpty()]
    [string]$Preset,

    [Parameter(Mandatory = $false, HelpMessage = "Elenca i preset disponibili.")]
    [switch]$ListPresets,

    [Parameter(Mandatory = $false, HelpMessage = "La modalità di selezione dei file: 'list', 'extensions', 'regex', 'InteractiveSelection'.")]
    [ValidateSet("list", "extensions", "regex", "InteractiveSelection")]
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

    [Parameter(Mandatory = $false, HelpMessage = "Array di percorsi di cartelle da escludere.")]
    [string[]]$ExcludePaths,

    [Parameter(Mandatory = $false, HelpMessage = "Array di nomi di file da escludere.")]
    [string[]]$ExcludeFiles,

    [Parameter(Mandatory = $false, HelpMessage = "Array di pattern regex per escludere i file.")]
    [string[]]$ExcludeFilePatterns,

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
    [string]$MaxSize,

    [Parameter(Mandatory = $false, HelpMessage = "Abilita la generazione del file di log.")]
    [switch]$EnableLog,

    [Parameter(Mandatory = $false, HelpMessage = "Mostra questo messaggio di aiuto.")]
    [switch]$Help
)

# Definizione dei preset
$Presets = @{
    "CSharp" = @{
        Mode = 'extensions'
        Extensions = '.cs', '.xaml'
        OutputFile = 'CombinedFile.cs'
        Recurse = $true
        ExcludePaths = 'Properties', 'obj', 'bin'
        ExcludeFilePatterns = '.*\.g\.i\.cs$', '.*\.g\.cs$', '.*\.designer\.cs$', '.*AssemblyInfo\.cs$', '^auto-generated'
    }
    # Puoi aggiungere altri preset qui
}

# Funzioni ausiliarie

function Write-Log {
    param (
        [string]$Message,
        [string]$Level = "INFO"
    )
    if ($EnableLog) {
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        $logMessage = "$timestamp [$Level] $Message"
        Add-Content -Path $logFile -Value $logMessage
    }
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

function Is-PathExcluded {
    param (
        [string]$FilePath,
        [string[]]$ExcludedPaths,
        [string[]]$ExcludedFiles,
        [string[]]$ExcludedFilePatterns
    )

    # Verifica esclusione per percorso
    foreach ($excluded in $ExcludedPaths) {
        if ($FilePath.StartsWith($excluded, [System.StringComparison]::InvariantCultureIgnoreCase)) {
            Write-Log "Escluso per percorso: $FilePath corrisponde a $excluded" "DEBUG"
            return $true
        }
    }

    # Verifica esclusione per nome file
    foreach ($excludedFile in $ExcludedFiles) {
        if ([System.IO.Path]::GetFileName($FilePath) -ieq $excludedFile) {
            Write-Log "Escluso per nome file: $FilePath corrisponde a $excludedFile" "DEBUG"
            return $true
        }
    }

    # Verifica esclusione per pattern regex
    foreach ($pattern in $ExcludedFilePatterns) {
        if ($FilePath -match $pattern) {
            Write-Log "Escluso per pattern regex: $FilePath corrisponde a $pattern" "DEBUG"
            return $true
        }
    }

    return $false
}

function Get-AllFiles {
    param (
        [string]$Path,
        [switch]$Recurse,
        [switch]$FollowReparsePoints,
        [string[]]$ExcludePaths = @(),
        [string[]]$ExcludeFiles = @(),
        [string[]]$ExcludeFilePatterns = @(),
        [switch]$EnableLog
    )

    $visitedPaths = @{}

    function Recursive-GetFiles {
        param (
            [string]$CurrentPath
        )

        # Evita di visitare lo stesso percorso più volte
        if ($visitedPaths.ContainsKey($CurrentPath.ToLower())) {
            return
        }
        $visitedPaths[$CurrentPath.ToLower()] = $true

        try {
            $items = Get-ChildItem -Path $CurrentPath -Force -ErrorAction Stop
        }
        catch {
            Write-Log "Errore durante l'accesso al percorso: $CurrentPath - $_" "WARNING"
            return
        }

        foreach ($item in $items) {
            # Salta i percorsi esclusi
            if ($ExcludePaths -and $ExcludePaths.Contains($item.FullName, [System.StringComparer]::InvariantCultureIgnoreCase)) {
                Write-Log "Percorso escluso: $($item.FullName)" "DEBUG"
                continue
            }

            if ($item.PSIsContainer) {
                # Controlla se è un reparse point (junction o symbolic link)
                if ($FollowReparsePoints -and ($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
                    # Risolvi il target della reparse point
                    $linkTarget = $item.LinkTarget
                    if ($linkTarget) {
                        # Se LinkTarget è relativo, risolvilo rispetto al CurrentPath
                        if (-not [System.IO.Path]::IsPathRooted($linkTarget)) {
                            $resolvedTarget = Resolve-Path -Path (Join-Path -Path $CurrentPath -ChildPath $linkTarget) -ErrorAction SilentlyContinue
                        }
                        else {
                            $resolvedTarget = Resolve-Path -Path $linkTarget -ErrorAction SilentlyContinue
                        }

                        if ($resolvedTarget) {
                            Write-Log "Navigando una directory reparse point: $($item.FullName) -> $($resolvedTarget.Path)" "DEBUG"
                            Recursive-GetFiles -CurrentPath $resolvedTarget.Path
                        }
                        else {
                            Write-Log "Impossibile risolvere il target della reparse point: $($item.FullName)" "WARNING"
                        }
                    }
                    else {
                        Write-Log "Reparse point senza target valido: $($item.FullName)" "WARNING"
                    }
                }
                elseif ($Recurse) {
                    Recursive-GetFiles -CurrentPath $item.FullName
                }
            }
            else {
                # File: verifica esclusioni
                if ($ExcludeFiles -and $ExcludeFiles.Contains([System.IO.Path]::GetFileName($item.FullName), [System.StringComparer]::InvariantCultureIgnoreCase)) {
                    Write-Log "File escluso: $($item.FullName)" "DEBUG"
                    continue
                }

                if ($ExcludeFilePatterns) {
                    $excluded = $false
                    foreach ($pattern in $ExcludeFilePatterns) {
                        if ($item.FullName -match $pattern) {
                            Write-Log "File escluso per pattern regex: $($item.FullName) corrisponde a $pattern" "DEBUG"
                            $excluded = $true
                            break
                        }
                    }
                    if ($excluded) {
                        continue
                    }
                }

                # Aggiungi il file
                Write-Log "File incluso: $($item.FullName)" "DEBUG"
                $item.FullName
            }
        }
    }

    Recursive-GetFiles -CurrentPath $Path
}

function Start-InteractiveSelection {
    param (
        [string[]]$InitialFiles,
        [string]$SourcePath
    )

    # Converti i percorsi assoluti in relativi
    $relativePaths = $InitialFiles | ForEach-Object { 
        try {
            [System.IO.Path]::GetRelativePath($SourcePath, $_)
        }
        catch {
            $_
        }
    }

    # Crea un file temporaneo
    $tempConfigFilePath = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "CombineFiles_InteractiveSelection.txt")

    # Scrivi l'elenco dei file nel file temporaneo
    $relativePaths | Out-File -FilePath $tempConfigFilePath -Encoding UTF8

    Write-Log "File di configurazione temporaneo creato: $tempConfigFilePath" "DEBUG"
    Show-Message "File di configurazione temporaneo creato: $tempConfigFilePath" "Yellow"

    # Apri il file nell'editor predefinito
    $editor = $env:EDITOR
    if (-not $editor) {
        $editor = "notepad.exe"
    }

    Write-Log "Apertura del file di configurazione temporaneo con l'editor: $editor" "DEBUG"
    Start-Process -FilePath $editor -ArgumentList $tempConfigFilePath -Wait

    Write-Log "Editor chiuso. Lettura del file di configurazione aggiornato." "DEBUG"
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

        Write-Log "File aggiornati dopo InteractiveSelection: $($updatedFiles.Count)" "DEBUG"
        Show-Message "File aggiornati dopo InteractiveSelection: $($updatedFiles.Count)" "Cyan"

        # Rimuovi il file temporaneo
        Remove-Item -Path $tempConfigFilePath -Force
        Write-Log "File di configurazione temporaneo rimosso: $tempConfigFilePath" "DEBUG"
    }
    else {
        Write-Error "Il file di configurazione temporaneo non è stato trovato: $tempConfigFilePath"
        Write-Log "Errore: Il file di configurazione temporaneo non è stato trovato: $tempConfigFilePath" "ERROR"
        Show-Message "Errore: Il file di configurazione temporaneo non è stato trovato." "Red"
        return @()
    }

    return $updatedFiles
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

function Show-Message {
    param (
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Funzione per mostrare l'aiuto personalizzato
function Show-CustomHelp {
    param ()

    Write-Host "Usage: .\Combine-Files.ps1 [options]" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Yellow
    Get-Help about_Script_Parameters | Select-Object -ExpandProperty Content
}

# Inizio dello script

# Definisci un percorso per il log solo se EnableLog è specificato
$sourcePath = Get-Location
if ($EnableLog) {
    $logFile = Join-Path -Path $sourcePath -ChildPath "CombineFiles.log"
    
    # Inizializza il log
    try {
        Out-File -FilePath $logFile -Force -Encoding UTF8
        Write-Log "Inizio operazione di combinazione file." "INFO"
    }
    catch {
        Write-Error "Impossibile creare il file di log: $logFile"
        Write-Host "Impossibile creare il file di log: $logFile" -ForegroundColor Red
        return
    }
}

# Gestione del parametro -Help
if ($Help) {
    Show-CustomHelp
    return
}

# Gestione del parametro -ListPresets
if ($ListPresets) {
    Show-Message "Preset disponibili:" "Cyan"
    foreach ($preset in $Presets.Keys) {
        Show-Message "- $preset" "Green"
    }
    Write-Log "Elenco dei preset richiesto dall'utente." "INFO"
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
                    elseif ($key -eq 'ExcludeFilePatterns') {
                        $ExcludeFilePatterns += $presetParams[$key]
                    }
                    # Aggiungi altri array se necessario
                }
                else {
                    # Imposta il valore del parametro se non è un array
                    Set-Variable -Name $key -Value $presetParams[$key]
                }
                Write-Log "Applicato preset '$Preset': $key = $($presetParams[$key])" "INFO"
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

Write-Log "Percorso sorgente: $sourcePath" "DEBUG"
Write-Log "File di output: $OutputFile" "DEBUG"

# Converti il percorso relativo del file di output in un percorso assoluto
if (-not [System.IO.Path]::IsPathRooted($OutputFile)) {
    $OutputFile = Join-Path -Path $sourcePath -ChildPath $OutputFile
}
Write-Log "Percorso assoluto del file di output: $OutputFile" "DEBUG"

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
                Write-Log "Percorso escluso aggiunto: $($resolved.Path)" "DEBUG"
            }
        }
        else {
            Write-Warning "Percorso di esclusione non trovato: $fullPath"
            Write-Log "Percorso di esclusione non trovato: $fullPath" "WARNING"
            Show-Message "Attenzione: Percorso di esclusione non trovato: $fullPath" "Magenta"
        }
    }
    Write-Log "Totale percorsi esclusi: $($fullExcludePaths.Count)" "DEBUG"
}

# Definisci i file da escludere
$fullExcludeFiles = @()
if ($ExcludeFiles) {
    foreach ($file in $ExcludeFiles) {
        $fullExcludeFiles += $file
        Write-Log "File escluso aggiunto: $file" "DEBUG"
    }
    Write-Log "Totale file esclusi: $($fullExcludeFiles.Count)" "DEBUG"
}

# Definisci i pattern di file da escludere
$fullExcludeFilePatterns = @()
if ($ExcludeFilePatterns) {
    foreach ($pattern in $ExcludeFilePatterns) {
        $fullExcludeFilePatterns += $pattern
        Write-Log "Pattern di file escluso aggiunto: $pattern" "DEBUG"
    }
    Write-Log "Totale pattern di file esclusi: $($fullExcludeFilePatterns.Count)" "DEBUG"
}

# Aggiungi l'OutputFile ai FullExcludeFiles basandosi sul nome
$outputFileName = [System.IO.Path]::GetFileName($OutputFile)
if ($outputFileName) {
    $fullExcludeFiles += $outputFileName
    Write-Log "Output file aggiunto ai nomi di file esclusi: $outputFileName" "DEBUG"
}

# Validazione dei parametri

if ($Mode -eq 'list' -and -not $FileList) {
    Write-Error "La modalità 'list' richiede il parametro -FileList."
    Write-Log "Errore: Modalità 'list' senza -FileList." "ERROR"
    Show-Message "Errore: La modalità 'list' richiede il parametro -FileList." "Red"
    return
}

if (($Mode -eq 'extensions' -or $Mode -eq 'InteractiveSelection') -and -not $Extensions) {
    Write-Error "La modalità '$Mode' richiede il parametro -Extensions."
    Write-Log "Errore: Modalità '$Mode' senza -Extensions." "ERROR"
    Show-Message "Errore: La modalità '$Mode' richiede il parametro -Extensions." "Red"
    return
}

if ($Mode -eq 'regex' -and -not $RegexPatterns) {
    Write-Error "La modalità 'regex' richiede il parametro -RegexPatterns."
    Write-Log "Errore: Modalità 'regex' senza -RegexPatterns." "ERROR"
    Show-Message "Errore: La modalità 'regex' richiede il parametro -RegexPatterns." "Red"
    return
}

# Validazione delle estensioni
if ($Mode -eq 'extensions' -or $Mode -eq 'InteractiveSelection') {
    foreach ($ext in $Extensions) {
        if (-not $ext.StartsWith('.')) {
            Write-Error "L'estensione '$ext' deve iniziare con un punto."
            Write-Log "Errore: Estensione non valida '$ext'." "ERROR"
            Show-Message "Errore: L'estensione '$ext' deve iniziare con un punto." "Red"
            return
        }
    }
}

# Converti le dimensioni in byte
$minSizeBytes = if ($MinSize) { Convert-SizeToBytes $MinSize } else { 0 }
$maxSizeBytes = if ($MaxSize) { Convert-SizeToBytes $MaxSize } else { [int64]::MaxValue }

# Ottieni la lista dei file da processare
Write-Log "Inizio raccolta dei file da processare." "INFO"
Show-Message "Inizio raccolta dei file da processare." "Cyan"

# Recupera i file secondo la modalità
switch ($Mode) {
    'list' {
        $filesToProcess = @()
        foreach ($file in $FileList) {
            $filePath = Join-Path -Path $sourcePath -ChildPath $file
            $resolved = Resolve-Path -Path $filePath -ErrorAction SilentlyContinue
            if ($resolved -and (Test-Path $resolved.Path -PathType Leaf)) {
                if (-not (Is-PathExcluded $resolved.Path $fullExcludePaths $fullExcludeFiles $fullExcludeFilePatterns)) {
                    # Escludi i file con contenuto auto-generated
                    if ((Get-Content -Path $resolved.Path -ErrorAction SilentlyContinue | Select-String -Pattern "<auto-generated>").Count -eq 0) {
                        $filesToProcess += $resolved.Path
                        Write-Log "File incluso dalla lista: $($resolved.Path)" "INFO"
                    }
                    else {
                        Write-Log "File escluso per contenuto auto-generated: $($resolved.Path)" "INFO"
                    }
                }
                else {
                    Write-Log "File escluso dalla lista o è il file di output: $($resolved.Path)" "INFO"
                }
            }
            else {
                Write-Log "File non trovato: $filePath" "WARNING"
                Show-Message "Avviso: File non trovato: $filePath" "Yellow"
            }
        }
    }
    'extensions' {
        $filesToProcess = Get-AllFiles -Path $sourcePath -Recurse:$Recurse -FollowReparsePoints:$Recurse -ExcludePaths $fullExcludePaths -ExcludeFiles $fullExcludeFiles -ExcludeFilePatterns $fullExcludeFilePatterns -EnableLog:$EnableLog
    }
    'regex' {
        $allFiles = Get-AllFiles -Path $sourcePath -Recurse:$Recurse -FollowReparsePoints:$Recurse -ExcludePaths $fullExcludePaths -ExcludeFiles $fullExcludeFiles -ExcludeFilePatterns $fullExcludeFilePatterns -EnableLog:$EnableLog
        $filesToProcess = @()
        foreach ($file in $allFiles) {
            foreach ($pattern in $RegexPatterns) {
                if ($file -match $pattern) {
                    # Escludi i file con contenuto auto-generated
                    if ((Get-Content -Path $file -ErrorAction SilentlyContinue | Select-String -Pattern "<auto-generated>").Count -eq 0) {
                        $filesToProcess += $file
                        Write-Log "File incluso per pattern regex: $file" "INFO"
                    }
                    else {
                        Write-Log "File escluso per contenuto auto-generated: $file" "INFO"
                    }
                    break  # Evita di aggiungere lo stesso file più volte se corrisponde a più pattern
                }
            }
        }
        $filesToProcess = $filesToProcess | Sort-Object -Unique
    }
    'InteractiveSelection' {
        # Ottieni tutti i file con le estensioni specificate
        $allFiles = Get-AllFiles -Path $sourcePath -Recurse:$Recurse -FollowReparsePoints:$Recurse -ExcludePaths $fullExcludePaths -ExcludeFiles $fullExcludeFiles -ExcludeFilePatterns $fullExcludeFilePatterns -EnableLog:$EnableLog
        # Filtra per estensioni
        $filesFiltered = @()
        foreach ($file in $allFiles) {
            foreach ($ext in $Extensions) {
                if ($file.EndsWith($ext, [System.StringComparison]::InvariantCultureIgnoreCase)) {
                    # Escludi i file con contenuto auto-generated
                    if ((Get-Content -Path $file -ErrorAction SilentlyContinue | Select-String -Pattern "<auto-generated>").Count -eq 0) {
                        $filesFiltered += $file
                        Write-Log "File incluso per estensione: $file" "INFO"
                    }
                    else {
                        Write-Log "File escluso per contenuto auto-generated: $file" "INFO"
                    }
                    break
                }
            }
        }
        $filesFiltered = $filesFiltered | Sort-Object -Unique
        # Avvia la selezione interattiva
        $filesToProcess = Start-InteractiveSelection -InitialFiles $filesFiltered -SourcePath $sourcePath
    }
    default {
        Write-Error "Modalità '$Mode' non riconosciuta o non supportata."
        Write-Log "Errore: Modalità '$Mode' non riconosciuta o non supportata." "ERROR"
        Show-Message "Errore: Modalità '$Mode' non riconosciuta o non supportata." "Red"
        return
    }
}

Write-Log "Numero iniziale di file da processare: $($filesToProcess.Count)" "INFO"
Show-Message "Numero iniziale di file da processare: $($filesToProcess.Count)" "Cyan"

# Gestione della modalità InteractiveSelection
if ($Mode -eq 'InteractiveSelection') {
    if ($filesToProcess.Count -eq 0) {
        Write-Warning "Nessun file selezionato dopo la selezione interattiva."
        Write-Log "Nessun file selezionato dopo la selezione interattiva." "WARNING"
        Show-Message "Nessun file selezionato dopo la selezione interattiva." "Yellow"
        return
    }

    Write-Log "Numero di file dopo InteractiveSelection: $($filesToProcess.Count)" "INFO"
    Show-Message "Numero di file dopo InteractiveSelection: $($filesToProcess.Count)" "Cyan"
}

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
    Write-Log "Totale file dopo filtraggio per data e dimensione: $($filesToProcess.Count)" "INFO"
    Show-Message "Totale file dopo filtraggio per data e dimensione: $($filesToProcess.Count)" "Cyan"
}

# Filtra nuovamente per escludere eventuali percorsi non gestiti
if ($fullExcludePaths -or $fullExcludeFiles -or $fullExcludeFilePatterns) {
    $filesToProcess = $filesToProcess | Where-Object {
        -not (Is-PathExcluded $_ $fullExcludePaths $fullExcludeFiles $fullExcludeFilePatterns)
    }
    Write-Log "Totale file da processare dopo esclusione finale: $($filesToProcess.Count)" "INFO"
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
    Write-Log "Trovati $($filesToProcess.Count) file da processare." "INFO"
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
        Write-Log "File di output creato/svuotato: $OutputFile" "INFO"
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

# Inizializza un hash table per tracciare i file già processati (per gestire hard link)
$processedFileHashes = @{}

# Processa ogni file
foreach ($filePath in $filesToProcess) {
    $currentFile++
    Write-Progress -Activity "Combinazione dei file" -Status "Elaborazione file $currentFile di $totalFiles" -PercentComplete (($currentFile / $totalFiles) * 100)

    # Calcola l'hash del file per verificare se è già stato processato (gestione hard link)
    try {
        $fileHash = Get-FileHash -Path $filePath -Algorithm SHA256 -ErrorAction Stop
        $hashString = $fileHash.Hash
    }
    catch {
        Write-Warning "Impossibile calcolare l'hash del file: $filePath"
        Write-Log "Impossibile calcolare l'hash del file: $filePath - $_" "WARNING"
        Show-Message "Attenzione: Impossibile calcolare l'hash del file: $filePath" "Magenta"
        continue
    }

    if ($processedFileHashes.ContainsKey($hashString)) {
        Write-Log "File già processato (hard link): $filePath" "DEBUG"
        continue
    }
    else {
        $processedFileHashes[$hashString] = $true
    }

    $fileName = [System.IO.Path]::GetFileName($filePath)
    $outputContent = if ($FileNamesOnly) { "### $fileName ###" } else { "### Contenuto di $fileName ###" }

    Write-OutputOrFile -Content $outputContent -OutputToConsole:$OutputToConsole -OutputFile $OutputFile -OutputFormat $OutputFormat

    if (-not $FileNamesOnly) {
        Write-Log "Aggiungendo contenuto di: $fileName" "INFO"
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
            Write-Log "File aggiunto correttamente: $fileName" "INFO"
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
    Write-Log "Operazione completata. Controlla il file '$OutputFile'." "INFO"
    Show-Message "Operazione completata. Controlla il file '$OutputFile'." "Green"
}
else {
    Write-Log "Operazione completata con output a console." "INFO"
    Show-Message "Operazione completata con output a console." "Green"
}
