using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CombineFiles.Core.Configuration;
using CombineFiles.Core.Infrastructure;
using CombineFiles.Core.Services;
using CombineFiles.Core.Helpers;

namespace CombineFiles.McpServer;

class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    static async Task Main(string[] args)
    {
        // MCP server comunica via stdio
        var stdin = Console.OpenStandardInput();
        var stdout = Console.OpenStandardOutput();
        var stderr = Console.OpenStandardError();

        // Redirect stderr per log (opzionale)
        var errorWriter = new StreamWriter(stderr, Encoding.UTF8) { AutoFlush = true };

        try
        {
            await RunMcpServer(stdin, stdout, errorWriter);
        }
        catch (Exception ex)
        {
            await errorWriter.WriteLineAsync($"Error: {ex.Message}");
            await errorWriter.FlushAsync();
            Environment.Exit(1);
        }
    }

    private static async Task RunMcpServer(Stream stdin, Stream stdout, StreamWriter stderr)
    {
        var reader = new StreamReader(stdin, Encoding.UTF8);
        var writer = new StreamWriter(stdout, Encoding.UTF8) { AutoFlush = true };

        string? serverInfo = null;

        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) break;

            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var request = JsonSerializer.Deserialize<McpRequest>(line, JsonOptions);
                if (request == null) continue;

                McpResponse? response = null;

                switch (request.Method)
                {
                    case "initialize":
                        response = HandleInitialize(request);
                        serverInfo = JsonSerializer.Serialize(response.Result, JsonOptions);
                        break;

                    case "tools/list":
                        response = HandleToolsList(request);
                        break;

                    case "tools/call":
                        response = await HandleToolsCall(request, stderr);
                        break;

                    case "ping":
                        response = new McpResponse { Id = request.Id, Result = new { Status = "pong" } };
                        break;

                    default:
                        response = new McpResponse
                        {
                            Id = request.Id,
                            Error = new McpError { Code = -32601, Message = $"Method not found: {request.Method}" }
                        };
                        break;
                }

                if (response != null)
                {
                    var responseJson = JsonSerializer.Serialize(response, JsonOptions);
                    await writer.WriteLineAsync(responseJson);
                }
            }
            catch (Exception ex)
            {
                var errorResponse = new McpResponse
                {
                    Id = null,
                    Error = new McpError { Code = -32603, Message = $"Internal error: {ex.Message}" }
                };
                var errorJson = JsonSerializer.Serialize(errorResponse, JsonOptions);
                await writer.WriteLineAsync(errorJson);
            }
        }
    }

    private static McpResponse HandleInitialize(McpRequest request)
    {
        var result = new
        {
            ProtocolVersion = "2024-11-05",
            Capabilities = new
            {
                Tools = new { }
            },
            ServerInfo = new
            {
                Name = "combinefiles-mcp-server",
                Version = "1.0.0"
            }
        };

        return new McpResponse { Id = request.Id, Result = result };
    }

    private static McpResponse HandleToolsList(McpRequest request)
    {
        var tools = new[]
        {
            new
            {
                Name = "combine_files",
                Description = "Combina più file in un unico file con opzioni di compattazione per LLM. Supporta filtri per estensioni, percorsi e limiti di token.",
                InputSchema = new
                {
                    Type = "object",
                    Properties = new
                    {
                        SourcePath = new { Type = "string", Description = "Percorso della cartella sorgente" },
                        Extensions = new { Type = "array", Items = new { Type = "string" }, Description = "Lista di estensioni da includere (es: [\".cs\", \".txt\"])" },
                        OutputFile = new { Type = "string", Description = "Percorso del file di output" },
                        MaxTokens = new { Type = "integer", Description = "Limite massimo di token (0 = illimitato)" },
                        MaxTokensPerPage = new { Type = "integer", Description = "Limite token per pagina quando si usa paginazione (0 = disabilitato)" },
                        CompactSpaces = new { Type = "boolean", Description = "Converte 4 spazi in 1 tab per ridurre token" },
                        CompactForLLM = new { Type = "boolean", Description = "Ottimizza output per LLM (rimuove righe vuote eccessive)" },
                        PaginateOutput = new { Type = "boolean", Description = "Abilita paginazione dell'output in più file" }
                    },
                    Required = new[] { "SourcePath", "OutputFile" }
                }
            }
        };

        return new McpResponse { Id = request.Id, Result = new { Tools = tools } };
    }

    private static async Task<McpResponse> HandleToolsCall(McpRequest request, StreamWriter stderr)
    {
        if (request.Params == null)
        {
            return new McpResponse
            {
                Id = request.Id,
                Error = new McpError { Code = -32602, Message = "Invalid params" }
            };
        }

        var toolName = request.Params.GetValueOrDefault("name")?.ToString();
        var arguments = request.Params.GetValueOrDefault("arguments") as Dictionary<string, object?>;

        if (toolName != "combine_files" || arguments == null)
        {
            return new McpResponse
            {
                Id = request.Id,
                Error = new McpError { Code = -32602, Message = "Invalid tool name or arguments" }
            };
        }

        try
        {
            var sourcePath = arguments.GetValueOrDefault("sourcePath")?.ToString() 
                ?? arguments.GetValueOrDefault("SourcePath")?.ToString();
            var outputFile = arguments.GetValueOrDefault("outputFile")?.ToString() 
                ?? arguments.GetValueOrDefault("OutputFile")?.ToString();
            var extensions = GetStringArray(arguments, "extensions", "Extensions");
            var maxTokens = GetInt(arguments, "maxTokens", "MaxTokens", 0);
            var maxTokensPerPage = GetInt(arguments, "maxTokensPerPage", "MaxTokensPerPage", 0);
            var compactSpaces = GetBool(arguments, "compactSpaces", "CompactSpaces", false);
            var compactForLLM = GetBool(arguments, "compactForLLM", "CompactForLLM", false);
            var paginateOutput = GetBool(arguments, "paginateOutput", "PaginateOutput", false);

            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(outputFile))
            {
                return new McpResponse
                {
                    Id = request.Id,
                    Error = new McpError { Code = -32602, Message = "SourcePath and OutputFile are required" }
                };
            }

            var result = await CombineFilesAsync(
                sourcePath,
                outputFile,
                extensions,
                maxTokens,
                maxTokensPerPage,
                compactSpaces,
                compactForLLM,
                paginateOutput,
                stderr);

            return new McpResponse
            {
                Id = request.Id,
                Result = new { Success = true, Message = result }
            };
        }
        catch (Exception ex)
        {
            return new McpResponse
            {
                Id = request.Id,
                Error = new McpError { Code = -32603, Message = $"Error combining files: {ex.Message}" }
            };
        }
    }

    private static async Task<string> CombineFilesAsync(
        string sourcePath,
        string outputFile,
        string[]? extensions,
        int maxTokens,
        int maxTokensPerPage,
        bool compactSpaces,
        bool compactForLLM,
        bool paginateOutput,
        StreamWriter stderr)
    {
        var logger = new Logger(enableLog: false);
        var fileCollector = new FileCollector(
            logger,
            new List<string>(),
            new List<string>(),
            new List<string>(),
            new List<string>());

        // Raccogli file
        var files = new List<string>();
        if (extensions != null && extensions.Length > 0)
        {
            var extList = extensions.Select(e => e.TrimStart('.')).ToList();
            var found = fileCollector.CollectFilesByExtensions(sourcePath, true, extList);
            files.AddRange(found);
        }
        else
        {
            // Se non ci sono estensioni specificate, raccogli tutti i file
            var result = fileCollector.GetAllFilesWithTokenInfo(sourcePath, true, null);
            files = result.IncludedFiles.Select(f => f.Path).ToList();
        }

        if (files.Count == 0)
        {
            return $"Nessun file trovato in {sourcePath}";
        }

        // Configura opzioni
        var options = new CombineFilesOptions
        {
            OutputFile = outputFile,
            OutputToConsole = false,
            MaxTotalTokens = maxTokens,
            MaxTokensPerPage = maxTokensPerPage,
            CompactSpacesToTabs = compactSpaces,
            CompactForLLM = compactForLLM,
            PartialFileMode = paginateOutput ? TokenLimitStrategy.PaginateOutput : TokenLimitStrategy.IncludePartial,
            MaxLinesPerFile = 0,
            ListOnlyFileNames = false
        };

        // Esegui merge
        if (paginateOutput && maxTokensPerPage > 0)
        {
            using var merger = new PaginatedFileMerger(
                logger,
                false,
                outputFile,
                false,
                0,
                maxTokensPerPage,
                maxTokens > 0 ? maxTokens : 0,
                compactSpaces,
                compactForLLM);

            foreach (var file in files)
            {
                merger.MergeFile(file);
            }

            merger.FinalizePages();
        }
        else
        {
            using var merger = new FileMerger(
                logger,
                false,
                outputFile,
                false,
                0,
                options.PartialFileMode,
                maxTokens > 0 ? maxTokens : 0,
                compactSpaces,
                compactForLLM);

            foreach (var file in files)
            {
                merger.MergeFile(file);
            }
        }

        return $"Combinati {files.Count} file in {outputFile}";
    }

    private static string[]? GetStringArray(Dictionary<string, object?> dict, string key1, string key2)
    {
        var value = dict.GetValueOrDefault(key1) ?? dict.GetValueOrDefault(key2);
        if (value == null) return null;

        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            return jsonElement.EnumerateArray().Select(e => e.GetString()).Where(s => s != null).Cast<string>().ToArray();
        }

        if (value is IEnumerable<object> enumerable)
        {
            return enumerable.Select(o => o?.ToString()).Where(s => s != null).Cast<string>().ToArray();
        }

        return null;
    }

    private static int GetInt(Dictionary<string, object?> dict, string key1, string key2, int defaultValue)
    {
        var value = dict.GetValueOrDefault(key1) ?? dict.GetValueOrDefault(key2);
        if (value == null) return defaultValue;

        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
        {
            return jsonElement.GetInt32();
        }

        if (value is int i) return i;
        if (int.TryParse(value.ToString(), out var parsed)) return parsed;

        return defaultValue;
    }

    private static bool GetBool(Dictionary<string, object?> dict, string key1, string key2, bool defaultValue)
    {
        var value = dict.GetValueOrDefault(key1) ?? dict.GetValueOrDefault(key2);
        if (value == null) return defaultValue;

        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.True || jsonElement.ValueKind == JsonValueKind.False)
            {
                return jsonElement.GetBoolean();
            }
        }

        if (value is bool b) return b;
        if (bool.TryParse(value.ToString(), out var parsed)) return parsed;

        return defaultValue;
    }
}

// Modelli per JSON-RPC
class McpRequest
{
    public string? JsonRpc { get; set; } = "2.0";
    public string? Id { get; set; }
    public string Method { get; set; } = string.Empty;
    public Dictionary<string, object?>? Params { get; set; }
}

class McpResponse
{
    public string? JsonRpc { get; set; } = "2.0";
    public string? Id { get; set; }
    public object? Result { get; set; }
    public McpError? Error { get; set; }
}

class McpError
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
}
