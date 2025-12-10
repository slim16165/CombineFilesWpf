namespace CombineFiles.Core.Configuration;

public class FileSearchConfig
{
    public bool IncludeSubfolders { get; set; }      // Se includere o meno le sottocartelle
    public bool ExcludeHidden { get; set; }         // Se escludere i file nascosti
    public string IncludeExtensions { get; set; }   // Estensioni da includere (es. ".txt,.cs")
    public string ExcludeExtensions { get; set; }   // Estensioni da escludere
    public string IncludePaths { get; set; }        // Percorsi da includere esplicitamente
    public string ExcludePaths { get; set; }        // Percorsi da escludere
}

public class FileMergeConfig
{
    public string OutputFolder { get; set; }
    public string OutputFileName { get; set; }      // Nome del file di output
    public bool OneFilePerExtension { get; set; }   // Se creare un file per ogni estensione
    public bool OverwriteFiles { get; set; }        // Se sovrascrivere i file esistenti
}