using System;
using System.IO;
using System.Text.RegularExpressions;

namespace CombineFiles.Core.Helpers;

public static class FileHelper
{
    /// <summary>
    /// Converte una stringa (es. "10MB", "1024") in byte.
    /// </summary>
    public static long ConvertSizeToBytes(string size)
    {
        if (string.IsNullOrWhiteSpace(size))
            return 0;

        size = size.Trim().ToUpperInvariant();
        var match = Regex.Match(size, @"^(\d+)\s*(KB|MB|GB)$", RegexOptions.IgnoreCase);

        if (match.Success)
        {
            long value = long.Parse(match.Groups[1].Value);
            string unit = match.Groups[2].Value.ToUpperInvariant();
            return unit switch
            {
                "KB" => value * 1024,
                "MB" => value * 1024 * 1024,
                "GB" => value * 1024 * 1024 * 1024,
                _ => throw new ArgumentException($"Unità sconosciuta: {unit}")
            };
        }

        if (Regex.IsMatch(size, @"^\d+$"))
        {
            return long.Parse(size);
        }

        throw new ArgumentException($"Formato di dimensione non riconosciuto: {size}");
    }

    /// <summary>
    /// Restituisce il percorso relativo.
    /// </summary>
    public static string GetRelativePath(string basePath, string targetPath)
    {
        Uri baseUri = new Uri(basePath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? basePath : basePath + Path.DirectorySeparatorChar);
        Uri targetUri = new Uri(targetPath);

        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(targetUri)
            .ToString().Replace('/', Path.DirectorySeparatorChar));
    }
}