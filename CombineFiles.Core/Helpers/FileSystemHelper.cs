using System;
using System.IO;
using System.Linq;

namespace CombineFiles.Core.Helpers;

public static class FileSystemHelper
{
    /// <summary>
    /// Copia una directory sorgente nella destinazione.
    /// </summary>
    /// <param name="sourceDir">Percorso della directory sorgente.</param>
    /// <param name="destinationDir">Percorso della directory di destinazione.</param>
    /// <param name="recursive">Se copiare anche le sottocartelle.</param>
    /// <param name="overwrite">Se sovrascrivere i file esistenti (default: false).</param>
    public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive, bool overwrite = false)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
        Directory.CreateDirectory(destinationDir);

        foreach (var file in dir.GetFiles())
        {
            string targetPath = Path.Combine(destinationDir, file.Name);
            try
            {
                file.CopyTo(targetPath, overwrite);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to copy {file.FullName} to {targetPath}: {ex.Message}", ex);
            }
        }

        if (recursive)
        {
            foreach (var subDir in dir.GetDirectories())
            {
                string newDestDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestDir, true, overwrite);
            }
        }
    }
    /// <summary>
    /// Sposta la directory o il file dalla posizione source a destination.
    /// </summary>
    public static void Move(string source, string destination, bool isDirectory)
    {
        if (isDirectory && Directory.Exists(destination) || !isDirectory && File.Exists(destination))
            throw new IOException($"Destination already exists: {destination}");
        if (isDirectory) Directory.Move(source, destination);
        else File.Move(source, destination);
    }

    /// <summary>
    /// Rinomina un file o una directory.
    /// </summary>
    public static void Rename(string source, string newName, bool isDirectory)
    {
        string parent = System.IO.Path.GetDirectoryName(source);
        string newPath = System.IO.Path.Combine(parent, newName);

        if (isDirectory)
        {
            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException($"Source directory not found: {source}");
            Directory.Move(source, newPath);
        }
        else
        {
            if (!File.Exists(source))
                throw new FileNotFoundException($"Source file not found: {source}");
            File.Move(source, newPath);
        }
    }

    /// <summary>
    /// Controlla se il nome è valido per un file o una directory.
    /// </summary>
    public static bool IsValidName(string name)
    {
        char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
        return !name.Any(c => invalidChars.Contains(c));
    }
}