# Architettura di CombineFiles

## Panoramica

**CombineFiles** è un'applicazione multi-interfaccia per combinare file di testo in modo efficiente. Il progetto è strutturato seguendo un'architettura modulare che separa la logica di business (Core) dalle interfacce utente (WPF, Console, MCP Server).

## Architettura a Livelli

```
┌─────────────────────────────────────────────────────────────┐
│                    INTERFACCE UTENTE                        │
├─────────────────┬─────────────────┬─────────────────────────┤
│  WPF App        │  Console App    │  MCP Server            │
│  (GUI)          │  (CLI)          │  (ChatGPT Integration) │
└────────┬────────┴────────┬────────┴────────┬────────────────┘
         │                  │                  │
         └──────────────────┼──────────────────┘
                           │
         ┌─────────────────▼─────────────────┐
         │      COMBINEFILES.CORE            │
         │  (Business Logic & Services)      │
         ├───────────────────────────────────┤
         │  • FileCollector                  │
         │  • FileMerger                     │
         │  • PaginatedFileMerger            │
         │  • FileFilterService              │
         │  • TokenEstimationService         │
         │  • Helpers & Utilities            │
         └───────────────────────────────────┘
```

## Componenti Principali

### 1. CombineFiles.Core

**Responsabilità**: Libreria condivisa contenente tutta la logica di business per la raccolta, filtraggio e unione di file.

**Target Framework**: .NET Standard 2.0 (compatibilità con .NET Framework e .NET Core/5+)

#### Struttura

```
CombineFiles.Core/
├── Configuration/
│   ├── CombineFilesOptions.cs      # Opzioni di configurazione
│   └── FileProcessingConfig.cs     # Configurazione elaborazione file
├── Services/
│   ├── FileCollector.cs           # Raccoglie file da directory
│   ├── FileMerger.cs              # Unisce file in un singolo output
│   ├── PaginatedFileMerger.cs    # Unisce file con paginazione
│   ├── FileFilterService.cs      # Servizio di filtraggio file
│   ├── FileOperationsService.cs  # Operazioni su file
│   ├── TokenEstimationService.cs # Stima token per LLM
│   └── AnalyticsService.cs       # Analisi e statistiche
├── Handlers/
│   ├── IFileContentHandler.cs    # Interfaccia handler contenuto
│   ├── DefaultContentHandler.cs  # Handler default
│   ├── CsvContentHandler.cs      # Handler CSV
│   └── JsonContentHandler.cs     # Handler JSON
├── Helpers/
│   ├── FileHelper.cs             # Utility per file
│   ├── FileCollectionHelper.cs   # Helper raccolta file
│   ├── FileFilterHelper.cs       # Helper filtraggio
│   ├── PathHelper.cs             # Utility percorsi
│   ├── EncodingDetector.cs       # Rilevamento encoding
│   ├── TextCompactor.cs          # Compattazione testo per LLM
│   └── ConsoleHelper.cs          # Utility console
├── Infrastructure/
│   ├── Logger.cs                 # Sistema di logging
│   └── LogLevel.cs               # Livelli di log
├── MCP/
│   ├── Client/                   # Client MCP (se presente)
│   └── Models/                   # Modelli MCP
└── PresetManager.cs              # Gestione preset configurazione
```

#### Servizi Chiave

**FileCollector**
- Raccoglie file da directory specificate
- Supporta filtri per estensioni, percorsi, pattern regex
- Stima token per ogni file
- Gestisce limiti di token globali

**FileMerger**
- Unisce file in un singolo output
- Supporta deduplicazione per hash
- Limiti per righe e token per file
- Compattazione spazi (4 spazi → 1 tab)
- Ottimizzazione output per LLM

**PaginatedFileMerger**
- Estende FileMerger per paginazione
- Divide output in più file quando si supera il limite token
- Utile per LLM con limiti di contesto

**TokenEstimationService**
- Stima numero di token per file
- Supporta diverse strategie di conteggio
- Utilizzato per rispettare limiti di contesto LLM

### 2. CombineFiles.ConsoleApp

**Responsabilità**: Interfaccia a riga di comando (CLI) per combinare file.

**Target Framework**: .NET 6.0+ / .NET 8.0+

#### Struttura

```
CombineFiles.ConsoleApp/
├── Program.cs                    # Entry point
├── Extensions/
│   ├── RootCommandBuilder.cs    # Costruzione comandi CLI
│   ├── OptionBuilder.cs         # Builder pattern per opzioni
│   └── ExecutionFlow.cs         # Flusso di esecuzione
├── Helpers/
│   ├── CombineFilesOptionsBinder.cs  # Binding opzioni
│   ├── CombineFilesOptionsBuilder.cs # Builder opzioni
│   └── ParameterHelper.cs       # Utility parametri
└── Interactive/
    ├── InteractiveMode.cs       # Modalità interattiva
    ├── InteractiveFolderSelector.cs
    └── InteractiveRegexInput.cs
```

#### Caratteristiche

- **System.CommandLine**: Utilizza System.CommandLine per parsing argomenti
- **Modalità Interattiva**: Wizard interattivo per guidare l'utente
- **Preset**: Supporto per preset configurazione predefiniti
- **Logging**: Output dettagliato con livelli configurabili

### 3. CombineFiles.Wpf

**Responsabilità**: Interfaccia grafica WPF per combinare file.

**Target Framework**: .NET Framework 4.7.2+

#### Struttura

```
CombineFiles.Wpf/
├── MainWindow.xaml/.cs          # Finestra principale
├── Controls/
│   ├── FileListControl.xaml    # Lista file selezionati
│   ├── FilterOptionsControl.xaml  # Opzioni filtraggio
│   └── OutputOptionsControl.xaml  # Opzioni output
└── Properties/
    └── Settings.settings        # Impostazioni applicazione
```

#### Caratteristiche

- **TreeView Integration**: Utilizza TreeViewFileExplorer per navigazione file
- **Filtri Avanzati**: Interfaccia per configurare filtri complessi
- **Anteprima**: Visualizzazione struttura file prima del merge
- **Configurazione**: Salvataggio/caricamento configurazioni JSON

### 4. CombineFiles.ShellUi

**Responsabilità**: Versione shell dell'applicazione WPF con integrazione nel menu contestuale di Windows.

**Target Framework**: .NET Framework 4.7.2+

#### Caratteristiche

- **Context Menu**: Integrazione nel menu contestuale di Windows
- **Registrazione**: Script .reg per registrazione nel registro di sistema

### 5. CombineFiles.McpServer

**Responsabilità**: Server MCP (Model Context Protocol) per integrazione con ChatGPT Desktop.

**Target Framework**: .NET 6.0+ / .NET 8.0+

#### Struttura

```
CombineFiles.McpServer/
├── Program.cs                   # Server MCP principale
├── README.md                    # Documentazione setup
├── SETUP.md                     # Guida configurazione
└── chatgpt-mcp-config.json     # Configurazione esempio
```

#### Caratteristiche

- **JSON-RPC 2.0**: Comunicazione via stdio con ChatGPT Desktop
- **Tool `combine_files`**: Tool MCP esposto a ChatGPT
- **Supporto Completo**: Tutte le opzioni di compattazione e paginazione

#### Protocollo MCP

Il server implementa il protocollo MCP:
- `initialize`: Inizializzazione connessione
- `tools/list`: Lista tool disponibili
- `tools/call`: Esecuzione tool `combine_files`
- `ping`: Health check

### 6. ExternalLibraries/TreeViewFileExplorer

**Responsabilità**: Libreria esterna per esplorazione file system con TreeView.

**Pattern**: MVVM (Model-View-ViewModel)

#### Componenti

- **ViewModels**: TreeViewExplorerViewModel, DirectoryViewModel, FileViewModel
- **Services**: FileSystemService, IconService, IconCacheService
- **Models**: FileItem, RelayCommand
- **Events**: EventAggregator per comunicazione decoupled

## Flussi di Esecuzione

### Flusso Console App

```
1. Program.Main()
   ↓
2. RootCommandBuilder.CreateRootCommand()
   ↓
3. Parse argomenti (System.CommandLine)
   ↓
4. CombineFilesOptionsBinder (binding opzioni)
   ↓
5. ExecutionFlow.Execute()
   ↓
6. FileCollector.CollectFiles()
   ↓
7. FileMerger / PaginatedFileMerger.MergeFile()
   ↓
8. Output file risultante
```

### Flusso WPF App

```
1. MainWindow (UI)
   ↓
2. Utente seleziona file/cartelle
   ↓
3. FileCollector.CollectFiles() (background)
   ↓
4. Visualizzazione anteprima (TreeView)
   ↓
5. Utente configura opzioni
   ↓
6. BtnStartMerging_Click()
   ↓
7. FileMerger.MergeFile() (async)
   ↓
8. Aggiornamento UI con progresso
```

### Flusso MCP Server

```
1. ChatGPT Desktop invia richiesta JSON-RPC
   ↓
2. Program.RunMcpServer() (loop stdio)
   ↓
3. Parse richiesta JSON-RPC
   ↓
4. HandleToolsCall() per "combine_files"
   ↓
5. CombineFilesAsync() (usa Core services)
   ↓
6. Risposta JSON-RPC con risultato
```

## Pattern e Principi di Design

### 1. Separation of Concerns

- **Core**: Logica di business indipendente da UI
- **UI Layer**: Solo presentazione e interazione utente
- **Services**: Servizi riutilizzabili e testabili

### 2. Dependency Injection (Parziale)

- Costruttori accettano dipendenze (Logger, Services)
- Iniezione manuale (non container DI completo)
- Facilita testabilità

### 3. Builder Pattern

- `OptionBuilder`: Costruzione opzioni CLI
- `RootCommandBuilder`: Costruzione comandi
- `CombineFilesOptionsBuilder`: Costruzione configurazione

### 4. Strategy Pattern

- `TokenLimitStrategy`: Strategie per gestione limiti token
  - `ExcludeCompletely`: Esclude file che superano limite
  - `IncludePartial`: Include file parzialmente
  - `PaginateOutput`: Pagina output in più file

- `FilePriorityStrategy`: Strategie per ordinamento file

### 5. Factory Pattern

- `PresetManager`: Crea preset configurazione
- Handler factory per diversi tipi di contenuto

## Gestione File e Encoding

### Rilevamento Encoding

Il sistema utilizza `EncodingDetector` per rilevare automaticamente l'encoding dei file:
- UTF-8 (con/senza BOM)
- UTF-16 (LE/BE)
- Windows-1252
- Fallback a UTF-8 se non rilevato

### Compattazione Testo

**TextCompactor** fornisce ottimizzazioni per LLM:
- `CompactSpacesToTabs()`: Converte 4 spazi consecutivi in 1 tab
- `CompactForLLM()`: Rimuove righe vuote eccessive, compatta header

## Gestione Token e Limitazioni

### Token Estimation

Il sistema stima token utilizzando:
- Conteggio parole (default)
- Conteggio caratteri (opzionale)
- Metriche personalizzate (estendibile)

### Limitazioni Supportate

1. **Per File**:
   - Limite righe (`MaxLinesPerFile`)
   - Limite token (`MaxTokensPerFile`)

2. **Globali**:
   - Limite token totale (`MaxTotalTokens`)
   - Limite token per pagina (`MaxTokensPerPage`)

3. **Strategie**:
   - `ExcludeCompletely`: Interrompe al primo file che supera limite
   - `IncludePartial`: Tronca file ma continua con successivi
   - `PaginateOutput`: Divide output in più file

## Logging e Diagnostica

### Logger

Sistema di logging semplice con livelli:
- `DEBUG`: Informazioni dettagliate
- `INFO`: Informazioni generali
- `WARNING`: Avvisi
- `ERROR`: Errori

### Output

- **Console App**: Output configurabile (normale/verbose/debug)
- **WPF App**: Logging in background (non bloccante UI)
- **MCP Server**: Logging su stderr (non interferisce con JSON-RPC)

## Estendibilità

### Handler Personalizzati

Implementare `IFileContentHandler` per supportare nuovi formati:
```csharp
public class CustomContentHandler : IFileContentHandler
{
    public bool CanHandle(string filePath) { ... }
    public string ProcessContent(string content) { ... }
}
```

### Preset Personalizzati

Creare preset configurazione tramite `PresetManager`:
```csharp
var preset = new CombineFilesOptions
{
    Extensions = new List<string> { ".cs", ".ts" },
    CompactSpacesToTabs = true,
    CompactForLLM = true
};
PresetManager.SavePreset("MyPreset", preset);
```

## Dipendenze Esterne

### CombineFiles.Core
- Nessuna dipendenza esterna (solo .NET Standard)

### CombineFiles.ConsoleApp
- `System.CommandLine`: Parsing argomenti CLI
- `Spectre.Console` (opzionale): Interfaccia interattiva avanzata

### CombineFiles.Wpf
- `Telerik UI for WPF`: Controlli TreeView avanzati
- `WindowsAPICodePack`: Dialog file/folder
- `Newtonsoft.Json`: Serializzazione configurazioni

### CombineFiles.McpServer
- Nessuna dipendenza esterna (solo .NET Core)

## Compatibilità e Target Framework

| Progetto | Target Framework | Piattaforma |
|----------|------------------|-------------|
| CombineFiles.Core | .NET Standard 2.0 | Cross-platform |
| CombineFiles.ConsoleApp | .NET 6.0+ / 8.0+ | Cross-platform |
| CombineFiles.Wpf | .NET Framework 4.7.2+ | Windows only |
| CombineFiles.ShellUi | .NET Framework 4.7.2+ | Windows only |
| CombineFiles.McpServer | .NET 6.0+ / 8.0+ | Cross-platform |

## Testing

### Unit Testing (Pianificato)

Struttura suggerita:
```
CombineFiles.Tests/
├── Core.Tests/
│   ├── FileMergerTests.cs
│   ├── FileCollectorTests.cs
│   └── TokenEstimationServiceTests.cs
└── ConsoleApp.Tests/
    └── CommandParsingTests.cs
```

## Performance

### Ottimizzazioni

1. **Streaming**: Utilizzo di `StreamReader`/`StreamWriter` per file grandi
2. **Lazy Loading**: Caricamento file solo quando necessario
3. **Hash Caching**: Cache hash per deduplicazione veloce
4. **Async Operations**: Operazioni I/O asincrone in WPF

### Limitazioni Note

- File molto grandi (>1GB) possono richiedere tempo significativo
- Paginazione con molti file può generare molti file output

## Sicurezza

### Considerazioni

1. **Path Validation**: Validazione percorsi per prevenire directory traversal
2. **File Access**: Controllo permessi lettura/scrittura
3. **Encoding Safety**: Gestione sicura encoding per prevenire injection

## Roadmap Architetturale

### Miglioramenti Pianificati

1. **Dependency Injection Container**: Integrazione container DI (Microsoft.Extensions.DependencyInjection)
2. **Plugin System**: Sistema plugin per estendere funzionalità
3. **API REST**: API REST per integrazione web
4. **Cloud Storage**: Supporto per AWS S3, Azure Blob Storage
5. **Distributed Processing**: Elaborazione distribuita per file molto grandi

## Conclusioni

L'architettura di CombineFiles è progettata per:
- **Modularità**: Componenti riutilizzabili e indipendenti
- **Estendibilità**: Facile aggiungere nuove funzionalità
- **Manutenibilità**: Codice organizzato e ben strutturato
- **Performance**: Ottimizzazioni per file grandi e operazioni batch
- **Compatibilità**: Supporto multi-piattaforma dove possibile

La separazione tra Core e UI permette di utilizzare la logica di business in diversi contesti (CLI, GUI, MCP Server) mantenendo coerenza e facilitando manutenzione.
