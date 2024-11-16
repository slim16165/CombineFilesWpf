# CombineFiles

## Descrizione

`CombineFiles` è un modulo PowerShell che permette di combinare il contenuto di più file o di stampare i nomi dei file in base alle opzioni specificate. Supporta preset predefiniti e modalità interattive per una maggiore flessibilità.

## Struttura del Progetto

CombineFiles/ ??? CombineFiles.psd1 # Manifesto del modulo ??? CombineFiles.psm1 # Modulo principale ??? Logging/ ? ??? Logging.psm1 # Funzioni di logging ??? FileSelection/ ? ??? FileSelection.psm1 # Funzioni per la selezione dei file ??? InteractiveSelection/ ? ??? InteractiveSelection.psm1 # Funzioni per la modalità interattiva ??? Utilities/ ? ??? Utilities.psm1 # Funzioni ausiliarie ??? README.md # Documentazione del progetto

markdown
Copia codice

## Installazione

1. **Clona il Repository o Copia i File**
   Assicurati che tutti i file e le cartelle siano presenti nella directory `CombineFiles`.

2. **Importa il Modulo in PowerShell**
   Apri PowerShell e naviga nella directory che contiene il modulo. Poi esegui:

   ```powershell
   Import-Module -Name ".\CombineFiles.psd1"
Utilizzo
Esempi
Utilizzare un Preset con InteractiveSelection

powershell
Copia codice
Start-CombineFiles -Preset 'CSharp' -Mode 'InteractiveSelection'
Questo comando:

Applica il preset CSharp.
Avvia la modalità interattiva per personalizzare l'elenco dei file da combinare.
Avviare la Modalità InteractiveSelection Senza Preset

powershell
Copia codice
Start-CombineFiles -Mode 'InteractiveSelection' -Extensions '.cs', '.xaml' -OutputFile 'CombinedFile.cs' -Recurse -ExcludePaths 'Properties', 'obj', 'bin' -ExcludeFilePatterns '.*\.g\.cs$', '.*\.designer\.cs$', '.*\.g\.i\.cs$'
Questo comando:

Seleziona i file con estensioni .cs e .xaml, ricorsivamente.
Esclude le cartelle Properties, obj, bin e i file che corrispondono ai pattern specificati.
Avvia la modalità interattiva per personalizzare l'elenco dei file da combinare.
Elencare i Preset Disponibili

powershell
Copia codice
Start-CombineFiles -ListPresets
Questo comando elenca tutti i preset disponibili definiti nel modulo.

Parametri Principali
-Preset: Nome del preset da utilizzare.
-ListPresets: Elenca i preset disponibili.
-Mode: Modalità di selezione dei file (list, extensions, regex, InteractiveSelection).
-FileList: Array di nomi di file da combinare (utilizzato con -Mode 'list').
-Extensions: Array di estensioni di file da includere (utilizzato con -Mode 'extensions' o -Mode 'InteractiveSelection').
-RegexPatterns: Array di pattern regex per selezionare i file (utilizzato con -Mode 'regex').
-OutputFile: Percorso del file di output.
-Recurse: Cerca anche nelle sottocartelle.
-FileNamesOnly: Stampa solo i nomi dei file invece del contenuto.
-OutputToConsole: Stampa a video invece di creare un file.
-ExcludePaths: Array di percorsi di cartelle da escludere.
-ExcludeFiles: Array di nomi di file da escludere.
-ExcludeFilePatterns: Array di pattern regex per escludere i file.
-OutputEncoding: Encoding del file di output (UTF8, ASCII, UTF7, UTF32, Unicode, Default).
-OutputFormat: Formato del file di output (txt, csv, json).
-MinDate: Data minima per i file da includere.
-MaxDate: Data massima per i file da includere.
-MinSize: Dimensione minima dei file (es. 1MB).
-MaxSize: Dimensione massima dei file (es. 10MB).