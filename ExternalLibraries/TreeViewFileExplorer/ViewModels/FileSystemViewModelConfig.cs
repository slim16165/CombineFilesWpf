using System.Text.RegularExpressions;

namespace TreeViewFileExplorer.ViewModels;

/// <summary>
/// Configurazione comune per i ViewModels del file system.
/// Riduce il numero di parametri nei costruttori.
/// </summary>
public class FileSystemViewModelConfig
{
    /// <summary>
    /// Indica se mostrare i file/cartelle nascosti.
    /// </summary>
    public bool ShowHiddenFiles { get; set; }

    /// <summary>
    /// Regex di filtro per i nomi dei file/cartelle (può essere null se non usato).
    /// </summary>
    public Regex? FilterRegex { get; set; }

    /// <summary>
    /// Crea una nuova configurazione con i valori specificati.
    /// </summary>
    public FileSystemViewModelConfig(bool showHiddenFiles, Regex? filterRegex = null)
    {
        ShowHiddenFiles = showHiddenFiles;
        FilterRegex = filterRegex;
    }

    /// <summary>
    /// Crea una configurazione di default (mostra file nascosti, nessun filtro).
    /// </summary>
    public static FileSystemViewModelConfig Default => new FileSystemViewModelConfig(true, null);
}
