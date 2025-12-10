# ⚙️ Configurazione ChatGPT Desktop - Guida Completa

## 📍 Passo 1: Compila il Server MCP

```bash
cd CombineFiles.McpServer
dotnet build -c Release
```

Verifica che il file eseguibile sia stato creato:
```
bin\Release\net8.0\CombineFiles.McpServer.dll
```

## 📍 Passo 2: Trova il Percorso del File di Configurazione

Su **Windows**, il file di configurazione MCP di ChatGPT Desktop si trova in:

```
%APPDATA%\ChatGPT\mcp.json
```

**Come aprirlo:**
1. Premi `Win + R`
2. Incolla: `%APPDATA%\ChatGPT`
3. Premi Invio
4. Se la cartella `ChatGPT` non esiste, creala
5. Crea il file `mcp.json` se non esiste

## 📍 Passo 3: Configura il File mcp.json

Apri o crea il file `mcp.json` e aggiungi questa configurazione:

```json
{
  "mcpServers": {
    "combinefiles": {
      "command": "dotnet",
      "args": [
        "C:\\PERCORSO\\COMPLETO\\AL\\TUO\\PROGETTO\\CombineFiles\\CombineFiles.McpServer\\bin\\Release\\net8.0\\CombineFiles.McpServer.dll"
      ]
    }
  }
}
```

### ⚠️ IMPORTANTE: Sostituisci il Percorso!

**Esempio per il tuo sistema:**
```json
{
  "mcpServers": {
    "combinefiles": {
      "command": "dotnet",
      "args": [
        "C:\\Users\\g.salvi\\Sviluppo\\Git Repository source\\CombineFiles\\CombineFiles.McpServer\\bin\\Release\\net8.0\\CombineFiles.McpServer.dll"
      ]
    }
  }
}
```

**Note:**
- Usa **doppi backslash** (`\\`) su Windows
- Il percorso deve essere **assoluto** (non relativo)
- Se hai altri server MCP già configurati, aggiungi `combinefiles` all'oggetto `mcpServers` esistente

### Esempio con più server MCP:

```json
{
  "mcpServers": {
    "filesystem": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-filesystem", "/path/to/allowed/files"]
    },
    "combinefiles": {
      "command": "dotnet",
      "args": [
        "C:\\Users\\g.salvi\\Sviluppo\\Git Repository source\\CombineFiles\\CombineFiles.McpServer\\bin\\Release\\net8.0\\CombineFiles.McpServer.dll"
      ]
    }
  }
}
```

## 📍 Passo 4: Riavvia ChatGPT Desktop

1. **Chiudi completamente** ChatGPT Desktop (non solo minimizza)
2. Riapri ChatGPT Desktop
3. Attendi qualche secondo per il caricamento dei server MCP

## 📍 Passo 5: Verifica l'Installazione

In ChatGPT Desktop, chiedi:

> "Quali tool MCP hai disponibile?"

oppure

> "Lista i tool MCP disponibili"

Dovresti vedere `combine_files` nella lista dei tool disponibili.

## 🎯 Esempi di Utilizzo

Una volta configurato, puoi usare CombineFiles direttamente in ChatGPT:

### Esempio 1: Combinazione base con compattazione
```
Combina tutti i file .cs dalla cartella C:\mio\progetto in un file chiamato output.txt con compattazione spazi e ottimizzazione per LLM
```

### Esempio 2: Combinazione con paginazione
```
Combina i file .cs e .ts da C:\progetto in combined.txt con limite di 100000 token per pagina, paginazione attiva e compattazione spazi
```

### Esempio 3: Combinazione semplice
```
Combina tutti i file .txt da C:\documenti in risultato.txt
```

## 🔧 Troubleshooting

### Il tool non appare in ChatGPT
- ✅ Verifica che il percorso nel file `mcp.json` sia corretto e assoluto
- ✅ Controlla che il file `.dll` esista nel percorso specificato
- ✅ Assicurati di aver compilato in modalità Release
- ✅ Riavvia completamente ChatGPT Desktop (chiudi tutte le finestre)

### Errori di esecuzione
- ✅ Verifica che .NET 8.0 SDK sia installato: `dotnet --version` (deve essere 8.0 o superiore)
- ✅ Controlla i log di ChatGPT Desktop per errori specifici

### File non trovati
- ✅ Verifica che il percorso della cartella sorgente sia corretto
- ✅ Assicurati di avere i permessi di lettura sulla cartella sorgente

### Problemi con il percorso
- ✅ Su Windows, usa sempre doppi backslash: `C:\\Users\\...`
- ✅ Il percorso deve essere assoluto, non relativo
- ✅ Evita spazi nel percorso se possibile (o usa le virgolette se necessario)

## 📝 Note Aggiuntive

- Il server MCP comunica via **stdio** usando JSON-RPC 2.0
- ChatGPT Desktop avvia automaticamente il processo quando necessario
- Il server rimane in esecuzione finché ChatGPT Desktop è aperto
- Puoi avere più server MCP configurati contemporaneamente

## 🆘 Supporto

Se hai problemi:
1. Controlla i log di ChatGPT Desktop
2. Verifica che .NET 8.0 SDK sia installato
3. Testa manualmente il server: `dotnet CombineFiles.McpServer.dll` (dovrebbe rimanere in attesa di input)
4. Consulta [README.md](README.md) per documentazione completa

