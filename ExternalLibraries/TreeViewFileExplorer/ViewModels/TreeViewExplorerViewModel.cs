using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using TreeViewFileExplorer.Events;

using TreeViewFileExplorer.Model;
using TreeViewFileExplorer.Services;

namespace TreeViewFileExplorer.ViewModels
{
    /// <summary>
    /// ViewModel for the TreeView explorer.
    /// </summary>
    public class TreeViewExplorerViewModel : INotifyPropertyChanged
    {
        private readonly IIconService _iconService;
        private readonly IFileSystemService _fileSystemService;
        private readonly IEventAggregator _eventAggregator;

        public TreeViewExplorerViewModel(IIconService iconService, IFileSystemService fileSystemService, IEventAggregator eventAggregator)
        {
            _iconService = iconService;
            _fileSystemService = fileSystemService;
            _eventAggregator = eventAggregator;
            RootItems = new ObservableCollection<IFileSystemObjectViewModel>();
            SelectedFiles = new ObservableCollection<FileItem>();
            InitializeRootItems();

            _eventAggregator.Subscribe<BeforeExploreEvent>(OnBeforeExplore);
            _eventAggregator.Subscribe<AfterExploreEvent>(OnAfterExplore);
        }

        public ObservableCollection<IFileSystemObjectViewModel> RootItems { get; }
        public ObservableCollection<FileItem> SelectedFiles { get; }

        private void InitializeRootItems()
        {
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                var driveViewModel = new DirectoryViewModel(drive.RootDirectory, _iconService, _fileSystemService, _eventAggregator);
                driveViewModel.PropertyChanged += OnFileSystemObjectPropertyChanged;
                RootItems.Add(driveViewModel);
            }
        }

        /// <summary>
        /// Handles property changes in file system objects.
        /// </summary>
        private void OnFileSystemObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IFileSystemObjectViewModel.IsSelected))
            {
                var item = sender as IFileSystemObjectViewModel;
                if (item != null)
                {
                    if (item.IsSelected)
                    {
                        AddToSelectedFiles(item);
                        _eventAggregator.Publish(new BeforeExploreEvent(item.Path));
                    }
                    else
                    {
                        RemoveFromSelectedFiles(item);
                    }
                }
            }
        }

        private void AddToSelectedFiles(IFileSystemObjectViewModel item)
        {
            if (item is FileViewModel file)
            {
                SelectedFiles.Add(new FileItem
                {
                    Name = file.Name,
                    Path = file.Path,
                    IsFolder = false
                });
            }
            else if (item is DirectoryViewModel dir)
            {
                SelectedFiles.Add(new FileItem
                {
                    Name = dir.Name,
                    Path = dir.Path,
                    IsFolder = true
                });
            }
            OnPropertyChanged(nameof(SelectedFiles));
        }

        private void RemoveFromSelectedFiles(IFileSystemObjectViewModel item)
        {
            var existingItem = SelectedFiles.FirstOrDefault(f => f.Path.Equals(item.Path, System.StringComparison.OrdinalIgnoreCase));
            if (existingItem != null)
            {
                SelectedFiles.Remove(existingItem);
                OnPropertyChanged(nameof(SelectedFiles));
            }
        }

        private void OnBeforeExplore(BeforeExploreEvent e)
        {
            // Implementa la logica prima di esplorare un percorso
            // Ad esempio, mostrare un messaggio di caricamento
        }

        private void OnAfterExplore(AfterExploreEvent e)
        {
            // Implementa la logica dopo aver esplorato un percorso
            // Ad esempio, nascondere un messaggio di caricamento
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
