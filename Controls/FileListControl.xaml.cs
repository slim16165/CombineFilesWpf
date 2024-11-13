using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using TreeViewFileExplorer;
using TreeViewFileExplorer.ShellClasses;

namespace CombineFilesWpf.Controls;

public partial class FileListControl : UserControl
{
    public ObservableCollection<FileItem> FileItems { get; set; }
    public ObservableCollection<FileItem> SelectedFiles { get; private set; } = new ObservableCollection<FileItem>();

    public FileListControl()
    {
        InitializeComponent();
        FileItems = new ObservableCollection<FileItem>();
        // Inizializzare i dati per il controllo TreeView personalizzato
        radTreeListViewFiles.DataContext = FileItems;
        radTreeListViewFiles.radTreeView.SelectionChanged += RadTreeView_SelectionChanged;
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

    private void RadTreeView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedFiles.Clear();
        foreach (var item in radTreeListViewFiles.SelectedItems)
        {
            if (item is FileSystemObjectInfo fso && fso.FileSystemInfo is FileInfo fileInfo)
            {
                var fileItem = new FileItem
                {
                    Name = fileInfo.Name,
                    Path = fileInfo.FullName
                    // Imposta altre proprietà se necessario
                };
                SelectedFiles.Add(fileItem);
            }
        }
    }

    // Metodo per disabilitare/abilitare i controlli
    public void ToggleControls(bool isEnabled)
    {
        radTreeListViewFiles.IsEnabled = isEnabled;
    }
}