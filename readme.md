# CombineFiles

> **Un'applicazione multi-interfaccia per combinare file in modo semplice ed efficiente**  
> Supporta interfacce WPF, Console e un modulo PowerShell standalone

## 🛠 Stato del Progetto
✅ Analisi preliminare completata  
✅ Core funzionale delle interfacce WPF e Console  
✅ Modulo PowerShell operativo (standalone e come porting in Console)  
🔍 Da migliorare: documentazione dettagliata e naming cartelle  
📌 Prossimi passi: rilascio della prima versione stabile e integrazione di nuove funzionalità

## 🚀 Funzionalità principali

### Applicazione WPF
- Selezione file/cartelle tramite interfaccia grafica  
- Filtraggio avanzato per estensioni ed esclusioni  
- Anteprima struttura file con TreeView (Telerik UI)  
- Unione file in formato testo (configurabile)

### Console App (Porting del modulo PowerShell)
- Modalità console per l'unione file (porting completo dello script PowerShell)  
- Supporto a operazioni batch con logging dettagliato  
- Gestione preset per scenari di combinazione complessi  
- Output simile a PowerShell, ma con messaggi più "puliti" in modalità normale (debug abilitabile)
- **Nuovo**: Compattazione spazi (4 spazi → 1 tab) per ridurre token
- **Nuovo**: Ottimizzazione output per LLM (ChatGPT, Claude, ecc.)
- **Nuovo**: Paginazione output basata su token

### Server MCP per ChatGPT Desktop 🆕
- Integrazione nativa con ChatGPT Desktop tramite MCP (Model Context Protocol)
- Tool `combine_files` disponibile direttamente in ChatGPT
- Supporto completo a tutte le opzioni di compattazione e paginazione
- Vedi [CombineFiles.McpServer/README.md](CombineFiles.McpServer/README.md) per setup completo

### Modulo PowerShell (standalone)
- **Indipendente**: il modulo PowerShell è stato spostato in un repository dedicato e può essere usato senza l'app WPF o la Console App.  
- Supporto a operazioni batch tramite script  
- Help contestuale (Get-Help) e configurazione preset integrata  
- Facile integrazione in workflow esistenti

## 📦 Installazione

### Applicazione WPF
```sh
git clone https://github.com/tuo-user/CombineFilesWpf.git
nuget restore
# Apri CombineFilesWpf.sln in Visual Studio e compila
```

### Console App
```sh
# Clona il repository
git clone https://github.com/tuo-user/CombineFilesWpf.git

# Apri la soluzione in Visual Studio e seleziona il progetto ConsoleApp (o esegui tramite CLI)
dotnet build CombineFiles.ConsoleApp
dotnet run --project CombineFiles.ConsoleApp -- [opzioni]
```

### Server MCP per ChatGPT Desktop 🆕
Integra CombineFiles direttamente in ChatGPT Desktop per combinare file tramite comandi naturali.

**Setup rapido:**
1. Compila il server: `dotnet build CombineFiles.McpServer -c Release`
2. Configura ChatGPT Desktop: vedi [SETUP.md](CombineFiles.McpServer/SETUP.md)
3. Riavvia ChatGPT Desktop
4. Usa: "Combina tutti i file .cs da C:\progetto in output.txt con compattazione LLM"

Vedi [CombineFiles.McpServer/README.md](CombineFiles.McpServer/README.md) per documentazione completa.

### Modulo PowerShell (standalone)
Lo script PowerShell ora è disponibile nel repository dedicato:
[CombineFiles-PowerShell](https://github.com/slim16165/CombineFiles-PowerShell/)

Per utilizzarlo:
```powershell
# 1. Clona il repository dedicato
git clone https://github.com/slim16165/CombineFiles-PowerShell.git

# 2. Importa il modulo
Import-Module .\CombineFiles-PowerShell\CombineFiles.psm1

# 3. Verifica i comandi disponibili
Get-Command -Module CombineFiles
```

## 🗂 Struttura del Progetto
```
CombineFilesWpf/
├── CombineFiles.Wpf/            # Applicazione principale WPF
├── CombineFiles.ConsoleApp/     # Console App: porting del modulo PowerShell
├── CombineFiles.McpServer/      # Server MCP per ChatGPT Desktop 🆕
└── docs/                        # Documentazione tecnica
    ├── ARCHITETTURA.md          # Documentazione architettura completa
    ├── PIANO_VALUTAZIONE_APPLICAZIONI.md  # Piano di valutazione maturità
    ├── REPORT_VALUTAZIONE_PRELIMINARE.md  # Report valutazione preliminare
    └── VALUTAZIONE_*.md         # Template valutazione per applicazione
```

> **Nota:** Il modulo PowerShell standalone si trova ora nel repository [CombineFiles-PowerShell](https://github.com/slim16165/CombineFiles-PowerShell/), mentre qui risiede il porting in C#.

## 💡 Utilizzo Base

### Console App (C#)
```sh
# Esempio di esecuzione base:
CombineFiles.ConsoleApp --Preset "CSharp" --SourcePath "C:\cartella-input" --OutputFile "CombinedFile.cs"

# Con compattazione spazi e ottimizzazione LLM:
CombineFiles.ConsoleApp --extensions .cs --compact-spaces --compact-llm --output-file output.txt

# Con paginazione basata su token:
CombineFiles.ConsoleApp --extensions .cs --partial-file-mode paginate --max-tokens-per-page 100000 --compact-spaces --compact-llm
```

### Modulo PowerShell
```powershell
# Combinazione base
Combine-Files -SourcePath "C:\cartella-input" -OutputFile "output-unificato.txt"

# Combinazione con filtri
Combine-Files -ExtensionFilter ".txt,.csv" -ExcludeHidden

# Combinazione con preset personalizzato
$myPreset = New-CombinationPreset -Encoding UTF8 -Separator "---"
Combine-Files -Preset $myPreset
```

## 📌 Decisioni Progettuali

### Console App
- **Porting**: La console app è il porting del modulo PowerShell, con output più “pulito” in modalità normale e dettagli aggiuntivi abilitabili in debug.
- **Log**: Supporta logging dettagliato con percorsi relativi, per un feedback simile a PowerShell.
- **Preset**: Supporta preset configurabili per operazioni batch.

### Modulo PowerShell
- **Indipendente**: Il modulo PowerShell è stato spostato in un repository dedicato (CombineFiles-PowerShell) e può essere utilizzato anche senza l'app WPF o la Console App.
- **Documentazione integrata**: Help contestuale via `Get-Help`.

### Formati Supportati
| Tipo       | Estensioni          | Note                          |
|------------|---------------------|-------------------------------|
| Testo      | .txt, .csv          | Encoding personalizzabile     |
| Logs       | .log, .md           | Supporto markdown             |
| Codice     | .cs, .ps1, .js      | Rimozione commenti opzionale  |

## 🔄 Workflow Consigliato
1. Seleziona files/cartelle tramite l'interfaccia WPF o inserisci il percorso in console  
2. Applica filtri e/o seleziona preset  
3. Genera un report di anteprima (opzionale)  
4. Esegui la combinazione  
5. Verifica i log operativi

## 📜 Roadmap 2024
- [ ] Aggiungere supporto per PDF e Office Documents (Q2)
- [ ] Implementare interfaccia CLI cross-platform (Q3)
- [ ] Integrazione con servizi cloud (AWS S3, Azure Blob) (Q4)

## ❓ FAQ
**D: Posso usare il modulo PowerShell senza l'app WPF o la Console App?**  
R: Assolutamente sì! Il modulo è completamente indipendente ed è disponibile nel repository [CombineFiles-PowerShell](https://github.com/slim16165/CombineFiles-PowerShell/).

**D: Come gestite i file di grandi dimensioni?**  
R: Usiamo stream reading/writing per minimizzare l'uso della memoria. Inoltre, supportiamo paginazione dell'output quando si supera un limite di token.

**D: Come posso ottimizzare l'output per ChatGPT o altri LLM?**  
R: Usa le opzioni `--compact-spaces` (converte 4 spazi in 1 tab) e `--compact-llm` (rimuove righe vuote eccessive e compatta header). Oppure usa il server MCP per integrazione diretta con ChatGPT Desktop.

**D: Come configuro ChatGPT Desktop per usare CombineFiles?**  
R: Vedi la guida completa in [CombineFiles.McpServer/SETUP.md](CombineFiles.McpServer/SETUP.md). In sintesi: compila il server, aggiungi la configurazione in `%APPDATA%\ChatGPT\mcp.json` e riavvia ChatGPT Desktop.

**D: È possibile contribuire al progetto?**  
R: Sì, consulta il file CONTRIBUTING.md per le linee guida.

## 📄 Licenza
MIT License - [Dettagli completi](LICENSE.md)

---

**Pronto per l'azione?** [Scarica l'ultima release](https://github.com/tuo-user/CombineFilesWpf/releases) o consulta la [documentazione architettura](docs/ARCHITETTURA.md).
