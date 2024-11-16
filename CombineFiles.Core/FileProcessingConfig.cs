using System;
using System.IO;

namespace CombineFiles.Core;

public class FileSearchConfig
{
    public bool IncludeSubfolders { get; set; }      // Se includere o meno le sottocartelle
    public bool ExcludeHidden { get; set; }         // Se escludere i file nascosti
    public string IncludeExtensions { get; set; }   // Estensioni da includere (es. ".txt,.cs")
    public string ExcludeExtensions { get; set; }   // Estensioni da escludere
    public string ExcludePaths { get; set; }        // Percorsi da escludere
}

public class FileMergeConfig
{
    private string _outputFolder;

    public string OutputFolder
    {
        get => _outputFolder;
        set => _outputFolder = Directory.Exists(value) ? value : throw new ArgumentException("Cartella di output non valida");
    }

    public string OutputFileName { get; set; }      // Nome del file di output
    public bool OneFilePerExtension { get; set; }   // Se creare un file per ogni estensione
    public bool OverwriteFiles { get; set; }        // Se sovrascrivere i file esistenti
}