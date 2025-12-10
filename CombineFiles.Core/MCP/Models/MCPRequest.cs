using System.Collections.Generic;

namespace CombineFiles.Core.MCP.Models;

/// <summary>
/// Modello per una richiesta MCP.
/// </summary>
public class MCPRequest
{
    public string Method { get; set; } = string.Empty;
    public Dictionary<string, object>? Params { get; set; }
    public string? Id { get; set; }
}

/// <summary>
/// Modello per una risposta MCP.
/// </summary>
public class MCPResponse
{
    public string? Id { get; set; }
    public object? Result { get; set; }
    public MCPError? Error { get; set; }
}

/// <summary>
/// Modello per un errore MCP.
/// </summary>
public class MCPError
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
}

