using System;
using System.IO;
using System.Text;

namespace CombineFiles.Core.Helpers;

/// <summary>
/// Utility per il rilevamento automatico dell'encoding di un file.
/// </summary>
public static class EncodingDetector
{
    /// <summary>
    /// Rileva l'encoding di un file leggendo il BOM (Byte Order Mark) o analizzando il contenuto.
    /// </summary>
    public static Encoding DetectEncoding(string filePath)
    {
        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return DetectEncoding(fileStream);
        }
        catch
        {
            return Encoding.UTF8; // Default fallback
        }
    }

    /// <summary>
    /// Rileva l'encoding da uno stream leggendo il BOM.
    /// </summary>
    public static Encoding DetectEncoding(Stream stream)
    {
        if (stream == null || stream.Length == 0)
            return Encoding.UTF8;

        long originalPosition = stream.Position;
        try
        {
            stream.Position = 0;

            // Leggi i primi 4 byte per il BOM
            byte[] bom = new byte[4];
            int bytesRead = stream.Read(bom, 0, 4);
            stream.Position = originalPosition;

            if (bytesRead < 2)
                return Encoding.UTF8;

            // UTF-8 BOM: EF BB BF
            if (bytesRead >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                return new UTF8Encoding(true);

            // UTF-16 LE BOM: FF FE
            if (bom[0] == 0xFF && bom[1] == 0xFE)
            {
                if (bytesRead >= 4 && bom[2] == 0x00 && bom[3] == 0x00)
                    return Encoding.UTF32; // UTF-32 LE
                return Encoding.Unicode; // UTF-16 LE
            }

            // UTF-16 BE BOM: FE FF
            if (bom[0] == 0xFE && bom[1] == 0xFF)
                return Encoding.BigEndianUnicode; // UTF-16 BE

            // UTF-32 BE BOM: 00 00 FE FF
            if (bytesRead >= 4 && bom[0] == 0x00 && bom[1] == 0x00 && bom[2] == 0xFE && bom[3] == 0xFF)
                return new UTF32Encoding(true, true);

            // Nessun BOM trovato, prova a rilevare dall'analisi del contenuto
            return DetectEncodingFromContent(stream);
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

    /// <summary>
    /// Tenta di rilevare l'encoding analizzando il contenuto del file.
    /// </summary>
    private static Encoding DetectEncodingFromContent(Stream stream)
    {
        try
        {
            long originalPosition = stream.Position;
            stream.Position = 0;

            // Leggi un campione del file (primi 1024 byte)
            byte[] buffer = new byte[Math.Min(1024, stream.Length)];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            stream.Position = originalPosition;

            if (bytesRead == 0)
                return Encoding.UTF8;

            // Prova UTF-8
            try
            {
                string utf8Test = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                // Se non ci sono caratteri di sostituzione, probabilmente è UTF-8
                if (utf8Test.IndexOf('\uFFFD') < 0)
                    return Encoding.UTF8;
            }
            catch { }

            // Prova Windows-1252 (Latin-1 esteso, comune in Europa)
            try
            {
                Encoding windows1252 = Encoding.GetEncoding(1252);
                windows1252.GetString(buffer, 0, bytesRead);
                return windows1252;
            }
            catch { }

            // Default: UTF-8
            return Encoding.UTF8;
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

    /// <summary>
    /// Converte il contenuto di un file da un encoding a un altro.
    /// </summary>
    public static string ConvertEncoding(string filePath, Encoding sourceEncoding, Encoding targetEncoding)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            string content = sourceEncoding.GetString(bytes);
            return content;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Errore nella conversione encoding di {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Legge un file usando l'encoding rilevato automaticamente.
    /// </summary>
    public static string ReadFileWithDetectedEncoding(string filePath)
    {
        Encoding encoding = DetectEncoding(filePath);
        return File.ReadAllText(filePath, encoding);
    }
}

