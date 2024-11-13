using System.Collections.ObjectModel;
using System.Windows.Controls;
using TreeViewFileExplorer;

namespace CombineFilesWpf.Controls;

public partial class FileListControl : UserControl
{
    public ObservableCollection<FileItem> FileItems { get; set; }

    public FileListControl()
    {
        InitializeComponent();
        FileItems = new ObservableCollection<FileItem>();
        // Inizializzare i dati per il controllo TreeView personalizzato
        radTreeListViewFiles.DataContext = FileItems;
    }

    // Metodo per aggiungere file
    public void AddFile(FileItem file)
    {
        FileItems.Add(file);
    }

    // Metodo per resettare la lista
    public void ClearFiles()
    {
        FileItems.Clear();
    }

    // Metodo per ottenere i file selezionati
    public ObservableCollection<FileItem> GetSelectedFiles()
    {
        var selectedItems = new ObservableCollection<FileItem>();

        // Logica personalizzata per ottenere file selezionati
        foreach (var item in radTreeListViewFiles.SelectedItems)  // Devi creare o trovare una proprietà SelectedItems
        {
            if (item is FileItem fileItem)
            {
                selectedItems.Add(fileItem);
            }
        }

        return selectedItems;
    }

    // Metodo per disabilitare/abilitare i controlli
    public void ToggleControls(bool isEnabled)
    {
        radTreeListViewFiles.IsEnabled = isEnabled;
    }
}