using System.Text.Json;

namespace CombineFiles.Core.Handlers;

public class JsonContentHandler : IFileContentHandler
{
    public string Handle(string content)
    {
        var doc = JsonDocument.Parse(content);
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }
}