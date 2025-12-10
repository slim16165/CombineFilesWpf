# CombineFiles MCP Server

Server MCP (Model Context Protocol) che espone CombineFiles come tool per ChatGPT Desktop.

## 🚀 Setup per ChatGPT Desktop

### 1. Compila il server MCP

```bash
cd CombineFiles.McpServer
dotnet build -c Release
```

### 2. Configura ChatGPT Desktop

Su **Windows**, il file di configurazione MCP si trova in:
```
%APPDATA%\ChatGPT\mcp.json
```

Crea o modifica questo file con il seguente contenuto (aggiorna il percorso con il tuo percorso effettivo):

```json
{
  "mcpServers": {
    "combinefiles": {
      "command": "dotnet",
      "args": [
        "C:\\percorso\\completo\\a\\CombineFiles.McpServer\\bin\\Release\\net8.0\\CombineFiles.McpServer.dll"
      ]
    }
  }
}
```

**Nota**: Sostituisci `C:\\percorso\\completo\\a\\` con il percorso effettivo del tuo progetto.

### 3. Riavvia ChatGPT Desktop

Chiudi e riapri ChatGPT Desktop per caricare la nuova configurazione.

### 4. Verifica l'installazione

In ChatGPT Desktop, dovresti vedere il tool `combine_files` disponibile. Puoi chiedere a ChatGPT:

> "Quali tool MCP hai disponibile?"

oppure

> "Combina tutti i file .cs dalla cartella C:\mio\progetto in un file ottimizzato per LLM"

## 📋 Tool Disponibile

### `combine_files`

Combina più file in un unico file con opzioni di compattazione per LLM.

**Parametri:**
- `sourcePath` (obbligatorio): Percorso della cartella sorgente
- `outputFile` (obbligatorio): Percorso del file di output
- `extensions` (opzionale): Array di estensioni da includere (es: `[".cs", ".txt"]`)
- `maxTokens` (opzionale): Limite massimo di token (0 = illimitato)
- `maxTokensPerPage` (opzionale): Limite token per pagina quando si usa paginazione
- `compactSpaces` (opzionale): Converte 4 spazi in 1 tab per ridurre token (default: false)
- `compactForLLM` (opzionale): Ottimizza output per LLM (default: false)
- `paginateOutput` (opzionale): Abilita paginazione dell'output in più file (default: false)

**Esempi di utilizzo:**

1. **Combinare tutti i file .cs con compattazione:**
   ```
   Combina tutti i file .cs da C:\mio\progetto in output.txt con compattazione spazi e ottimizzazione LLM
   ```

2. **Combinare con limite token e paginazione:**
   ```
   Combina i file .cs da C:\progetto in output.txt con limite di 100000 token per pagina e paginazione attiva
   ```

3. **Combinare file specifici:**
   ```
   Combina file .cs e .ts da C:\progetto in combined.txt
   ```

## 🔧 Sviluppo

### Test locale

Puoi testare il server MCP manualmente:

```bash
# Esegui il server
dotnet run --project CombineFiles.McpServer

# Invia una richiesta di test (da un altro terminale)
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' | dotnet run --project CombineFiles.McpServer
```

### Debug

Il server scrive errori su stderr, che puoi vedere nei log di ChatGPT Desktop.

## 📝 Note

- Il server MCP comunica via stdio usando JSON-RPC 2.0
- Assicurati che .NET 8.0 SDK sia installato
- Il percorso nel file di configurazione deve essere assoluto e usare doppi backslash (`\\`) su Windows

