// FileListControl.xaml.cs

using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using TreeViewFileExplorer.Model;

namespace CombineFilesWpf.Controls
{
    public partial class FileListControl : UserControl
    {
        // FileItems potrebbe non essere più necessario se gestito da TreeViewFileExplorerCustom
        // public ObservableCollection<FileItem> FileItems { get; set; }

        public ObservableCollection<FileItem> SelectedFiles { get; private set; } = new ObservableCollection<FileItem>();

        public FileListControl()
        {
            InitializeComponent();
            // Inizializzare i dati per il controllo TreeView personalizzato
            // radTreeListViewFiles.DataContext = FileItems; // Non necessario se gestito internamente
            // radTreeListViewFiles.radTreeView.SelectionChanged += RadTreeView_SelectionChanged; // Rimosso
        }

        // Metodo per aggiungere file
        public void AddFile(FileItem file)
        {
            // Potrebbe non essere necessario se TreeViewFileExplorerCustom gestisce l'aggiunta dei file
            // Se necessario, implementa un metodo nel TreeViewFileExplorerCustom per aggiungere file
        }

        // Metodo per resettare la lista
        public void ClearFiles()
        {
            // Potrebbe non essere necessario se TreeViewFileExplorerCustom gestisce la pulizia
            // Se necessario, implementa un metodo nel TreeViewFileExplorerCustom per pulire i file
        }

        // Evento sollevato quando SelectedFiles cambia nel TreeViewFileExplorerCustom
        private void TreeViewFileExplorer_SelectedFilesChanged(object sender, EventArgs e)
        {
            // Aggiorna la collezione SelectedFiles nel FileListControl
            SelectedFiles.Clear();
            foreach (var file in treeViewFileExplorer.SelectedFiles)
            {
                SelectedFiles.Add(file);
            }

            // Puoi aggiungere ulteriori logiche qui, ad esempio aggiornare una ListBox o altre UI
        }

        // Metodo per disabilitare/abilitare i controlli
        public void ToggleControls(bool isEnabled)
        {
            treeViewFileExplorer.IsEnabled = isEnabled;
            // Altri controlli possono essere abilitati/disabilitati qui
        }
    }
}
