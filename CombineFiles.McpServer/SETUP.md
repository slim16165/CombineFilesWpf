# 🚀 Setup Rapido - CombineFiles MCP Server per ChatGPT Desktop

## Passo 1: Compila il Server

```bash
cd CombineFiles.McpServer
dotnet build -c Release
```

Il file eseguibile sarà in: `bin\Release\net8.0\CombineFiles.McpServer.dll`

## Passo 2: Configura ChatGPT Desktop

1. **Trova il percorso del file di configurazione:**
   - Su Windows: `%APPDATA%\ChatGPT\mcp.json`
   - Apri Esplora File e incolla: `%APPDATA%\ChatGPT` nella barra degli indirizzi

2. **Crea o modifica `mcp.json`** con questo contenuto:

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

**⚠️ IMPORTANTE:** Sostituisci il percorso con il percorso assoluto del tuo file `.dll`!

3. **Riavvia ChatGPT Desktop** completamente (chiudi e riapri)

## Passo 3: Verifica

Apri ChatGPT Desktop e chiedi:

> "Quali tool MCP hai disponibile?"

Dovresti vedere `combine_files` nella lista.

## Esempio di Utilizzo

Una volta configurato, puoi chiedere a ChatGPT:

> "Combina tutti i file .cs dalla cartella C:\mio\progetto in un file chiamato output.txt con compattazione spazi e ottimizzazione per LLM"

oppure

> "Combina i file .cs e .ts da C:\progetto in combined.txt con limite di 100000 token per pagina e paginazione attiva"

## Troubleshooting

- **Il tool non appare**: Verifica che il percorso nel file `mcp.json` sia corretto e assoluto
- **Errori di esecuzione**: Controlla che .NET 8.0 SDK sia installato (`dotnet --version`)
- **File non trovati**: Verifica che il percorso della cartella sorgente sia corretto

