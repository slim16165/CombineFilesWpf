// DirectoryViewModel.cs
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using TreeViewFileExplorer.Enums;
using TreeViewFileExplorer.Events;
using TreeViewFileExplorer.Services;

namespace TreeViewFileExplorer.ViewModels
{
    /// <summary>
    /// ViewModel for directories.
    /// </summary>
    public class DirectoryViewModel : BaseFileSystemObjectViewModel
    {
        private ImageSource _imageSource;
        private readonly IEventAggregator _eventAggregator;
        private readonly bool _showHiddenFiles;
        private readonly Regex _filterRegex;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryViewModel"/> class.
        /// </summary>
        public DirectoryViewModel(DirectoryInfo directoryInfo, IIconService iconService, IFileSystemService fileSystemService, IEventAggregator eventAggregator, bool showHiddenFiles, Regex filterRegex)
            : base(iconService, fileSystemService, showHiddenFiles, filterRegex)
        {
            _eventAggregator = eventAggregator;
            _showHiddenFiles = showHiddenFiles;
            _filterRegex = filterRegex;
            Name = directoryInfo.Name;
            Path = directoryInfo.FullName;
            _imageSource = iconService.GetIcon(Path, ItemType.Folder, IconSize.Small, ItemState.Close);
            Children.Add(new DummyViewModel());
        }

        public override string Name { get; protected set; }
        public override string Path { get; protected set; }

        public override ImageSource ImageSource
        {
            get => _imageSource;
            protected set
            {
                _imageSource = value;
                OnPropertyChanged();
            }
        }

        // DirectoryViewModel.cs
        public override async Task ExploreAsync()
        {
            if (Children.Count == 1 && Children[0] is DummyViewModel)
            {
                _eventAggregator.Publish(new BeforeExploreEvent(Path));

                Children.Clear();

                try
                {
                    var directories = await FileSystemService.GetDirectoriesAsync(Path, _showHiddenFiles, _filterRegex);
                    foreach (var dir in directories)
                    {
                        var dirViewModel = new DirectoryViewModel(dir, IconService, FileSystemService, _eventAggregator, _showHiddenFiles, _filterRegex);
                        dirViewModel.PropertyChanged += OnChildPropertyChanged;
                        Children.Add(dirViewModel);
                    }

                    var files = await FileSystemService.GetFilesAsync(Path, _showHiddenFiles, _filterRegex);
                    foreach (var file in files)
                    {
                        var fileViewModel = new FileViewModel(file, IconService, FileSystemService, _showHiddenFiles, _filterRegex);
                        fileViewModel.PropertyChanged += OnChildPropertyChanged;
                        Children.Add(fileViewModel);
                    }

                    ImageSource = IconService.GetIcon(Path, ItemType.Folder, IconSize.Small, ItemState.Open);
                }
                catch (Exception ex)
                {
                    // Log o gestisci l'eccezione
                }
                finally
                {
                    _eventAggregator.Publish(new AfterExploreEvent(Path));
                }
            }
        }


        private void OnChildPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsSelected))
            {
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }
}
