# 📂 CombineFilesWpf 

> **Un'applicazione WPF + modulo PowerShell per combinare file in modo semplice ed efficiente**

## 🛠 Stato del Progetto
✅ Analisi preliminare completata  
✅ Core funzionale dell'applicazione WPF  
✅ Modulo PowerShell operativo  
🔍 Da migliorare: documentazione e naming cartelle  
📌 Prossimi passi: rilasciare prima versione stabile

## 🚀 Funzionalità principali
### Applicazione WPF
✅ Selezione file/cartelle con interfaccia grafica  
✅ Filtraggio avanzato per estensione  
✅ Anteprima struttura file con TreeView (Telerik UI)  
✅ Unione file in formato testo (configurabile)

### Modulo PowerShell
✅ Utilizzo standalone senza interfaccia grafica  
✅ Supporto a operazioni batch  
✅ Logging dettagliato delle operazioni  
✅ Presets configurabili per scenari complessi

## 📦 Installazione
### Applicazione WPF
```sh
git clone https://github.com/tuo-user/CombineFilesWpf.git
nuget restore
# Apri CombineFilesWpf.sln in Visual Studio e compila
```

### Modulo PowerShell (standalone)
```powershell
# 1. Clona il repository
git clone https://github.com/tuo-user/CombineFilesWpf.git

# 2. Importa il modulo
Import-Module .\powershell-module\CombineFiles-Module\CombineFiles.psm1

# 3. Verifica i comandi disponibili
Get-Command -Module CombineFiles
```

## 🗂 Struttura del Progetto
```
CombineFilesWpf/
├── CombineFiles.Wpf/          # Applicazione principale WPF
├── powershell-module/         # Modulo standalone (ex script-powershell)
│   ├── CombineFiles-Module/   # Modulo principale (ex PowerShellModuleProject1)
│   │   ├── FileSelection/     # Logiche di selezione file
│   │   ├── Presets.psm1       # Configurazioni predefinite
│   │   └── Main.ps1           # Script di esempio
└── docs/                      # Documentazione tecnica
```

## 💡 Utilizzo Base (PowerShell)
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
### Modulo PowerShell
✅ **Rinominata cartella** in `powershell-module` per maggiore chiarezza  
✅ **Namespace unificato**: `CombineFiles` per tutte le funzioni  
✅ **Documentazione integrata**: Help contestuale via `Get-Help`

### Formati Supportati
| Tipo       | Estensioni      | Note                  |
|------------|-----------------|-----------------------|
| Testo      | .txt, .csv      | Encoding personalizzabile |
| Logs       | .log, .md       | Supporto markdown     |
| Codice     | .cs, .ps1, .js  | Rimozione commenti    |

## 🔄 Workflow Consigliato
1. Seleziona files/cartelle
2. Applica filtri (opzionale)
3. Scegli preset o crea configurazione
4. Genera report di anteprima
5. Esegui combinazione
6. Verifica log operazioni

## 📜 Roadmap 2024
- [ ] Aggiungere supporto per PDF e Office Documents (Q2)
- [ ] Implementare interfaccia CLI cross-platform (Q3)
- [ ] Integrazione con servizi cloud (AWS S3, Azure Blob) (Q4)

## ❓ FAQ
**D: Posso usare il modulo PowerShell senza l'app WPF?**  
R: Assolutamente sì! Il modulo è completamente indipendente.

**D: Come gestite i file di grandi dimensioni?**  
R: Usiamo stream reading/writing per evitare carichi di memoria.

**D: È possibile contribuire al progetto?**  
R: Sì, consulta CONTRIBUTING.md per le linee guida.

## 📄 Licenza
MIT License - [Dettagli completi](LICENSE.md)

---

**Pronto per l'azione?** [Scarica l'ultima release](https://github.com/tuo-user/CombineFilesWpf/releases) o consulta la [documentazione avanzata](docs/advanced.md).
