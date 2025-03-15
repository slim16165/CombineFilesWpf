using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace CombineFiles.Core.Handlers;

public class CsvContentHandler : IFileContentHandler
{
    private readonly bool _hasHeaders;

    public CsvContentHandler(bool hasHeaders = true) => _hasHeaders = hasHeaders;

    public string Handle(string content)
    {
        // Configurazione del CsvReader impostando la presenza o meno dell'intestazione
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = _hasHeaders,
        };

        using var reader = new StringReader(content);
        using var csv = new CsvReader(reader, config);

        // Legge tutti i record dal CSV
        var records = csv.GetRecords<dynamic>().ToList();
        if (!records.Any())
            return string.Empty;

        var table = new StringBuilder();
        List<string> headers = new List<string>();

        if (_hasHeaders)
        {
            // Estrae le intestazioni dalle chiavi del primo record
            var firstRecord = (IDictionary<string, object>)records.First();
            headers = firstRecord.Keys.ToList();
            table.AppendLine(string.Join(" | ", headers));
            table.AppendLine(new string('-', headers.Count * 4));
        }

        // Itera sui record e costruisce le righe della tabella
        foreach (var record in records)
        {
            var dict = (IDictionary<string, object>)record;
            if (_hasHeaders)
            {
                // Garantisce l'ordine delle colonne secondo le intestazioni
                var values = headers.Select(header => dict[header]);
                table.AppendLine(string.Join(" | ", values));
            }
            else
            {
                table.AppendLine(string.Join(" | ", dict.Values));
            }
        }

        return table.ToString();
    }
}