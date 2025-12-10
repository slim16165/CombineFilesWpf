using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CombineFiles.Core.MCP.Models;

namespace CombineFiles.Core.MCP.Client;

/// <summary>
/// Esecutore di tool MCP per operazioni avanzate.
/// </summary>
public class MCPToolExecutor
{
    private readonly MCPClient _client;

    public MCPToolExecutor(MCPClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Esegue il tool analyze_file_structure per analizzare la struttura del progetto.
    /// </summary>
    public async Task<object?> AnalyzeFileStructureAsync(string projectPath)
    {
        if (!_client.IsAvailable)
            return null;

        var request = new MCPRequest
        {
            Method = "tools/call",
            Params = new Dictionary<string, object>
            {
                { "name", "analyze_file_structure" },
                { "arguments", new Dictionary<string, object> { { "path", projectPath } } }
            }
        };

        var response = await _client.SendRequestAsync(request);
        return response.Result;
    }

    /// <summary>
    /// Esegue il tool generate_smart_preset per generare preset intelligenti.
    /// </summary>
    public async Task<object?> GenerateSmartPresetAsync(string projectPath)
    {
        if (!_client.IsAvailable)
            return null;

        var request = new MCPRequest
        {
            Method = "tools/call",
            Params = new Dictionary<string, object>
            {
                { "name", "generate_smart_preset" },
                { "arguments", new Dictionary<string, object> { { "path", projectPath } } }
            }
        };

        var response = await _client.SendRequestAsync(request);
        return response.Result;
    }

    /// <summary>
    /// Esegue il tool suggest_merge_strategy per suggerire strategia di merge.
    /// </summary>
    public async Task<object?> SuggestMergeStrategyAsync(List<string> filePaths, int maxTokens)
    {
        if (!_client.IsAvailable)
            return null;

        var request = new MCPRequest
        {
            Method = "tools/call",
            Params = new Dictionary<string, object>
            {
                { "name", "suggest_merge_strategy" },
                { "arguments", new Dictionary<string, object>
                    {
                        { "files", filePaths },
                        { "maxTokens", maxTokens }
                    }
                }
            }
        };

        var response = await _client.SendRequestAsync(request);
        return response.Result;
    }
}

