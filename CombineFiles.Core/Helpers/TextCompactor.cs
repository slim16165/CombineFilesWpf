using System.Text;

namespace CombineFiles.Core.Helpers;

/// <summary>
/// Helper per compattare il testo per ottimizzare l'uso con LLM come ChatGPT.
/// </summary>
public static class TextCompactor
{
    /// <summary>
    /// Compatta gli spazi: converte 4 spazi consecutivi in 1 tab.
    /// </summary>
    public static string CompactSpacesToTabs(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new StringBuilder(text.Length);
        int spaceCount = 0;

        foreach (char c in text)
        {
            if (c == ' ')
            {
                spaceCount++;
                if (spaceCount == 4)
                {
                    sb.Append('\t');
                    spaceCount = 0;
                }
            }
            else
            {
                // Scrivi gli spazi rimanenti prima di aggiungere il carattere
                if (spaceCount > 0)
                {
                    for (int i = 0; i < spaceCount; i++)
                        sb.Append(' ');
                    spaceCount = 0;
                }
                sb.Append(c);
            }
        }

        // Aggiungi eventuali spazi rimanenti alla fine
        if (spaceCount > 0)
        {
            for (int i = 0; i < spaceCount; i++)
                sb.Append(' ');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Rimuove righe vuote eccessive (più di 2 consecutive) e compatta gli header.
    /// Ottimizzato per l'uso con LLM.
    /// </summary>
    public static string CompactForLLM(string text, bool compactHeaders = true)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        var result = new List<string>();
        int emptyLineCount = 0;

        foreach (var line in lines)
        {
            bool isEmpty = string.IsNullOrWhiteSpace(line);

            if (isEmpty)
            {
                emptyLineCount++;
                // Mantieni massimo 1 riga vuota consecutiva
                if (emptyLineCount <= 1)
                    result.Add("");
            }
            else
            {
                emptyLineCount = 0;
                string processedLine = line;

                // Compatta header se richiesto
                if (compactHeaders)
                {
                    // Semplifica header tipo "### Contenuto di ... ###"
                    if (line.Trim().StartsWith("###") && line.Trim().EndsWith("###"))
                    {
                        processedLine = line.Trim()
                            .Replace("### Contenuto di ", "### ")
                            .Replace(" ###", " ###");
                    }
                }

                result.Add(processedLine);
            }
        }

        return string.Join(Environment.NewLine, result);
    }

    /// <summary>
    /// Applica tutte le trasformazioni di compattazione.
    /// </summary>
    public static string CompactAll(string text, bool compactSpaces = true, bool compactForLLM = true)
    {
        string result = text;

        if (compactSpaces)
            result = CompactSpacesToTabs(result);

        if (compactForLLM)
            result = CompactForLLM(result);

        return result;
    }
}

