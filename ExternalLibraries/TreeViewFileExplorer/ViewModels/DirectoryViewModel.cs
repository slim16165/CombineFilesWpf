using System.IO;
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
        private readonly DirectoryInfo _directoryInfo;
        private ImageSource _imageSource;
        private readonly IEventAggregator _eventAggregator;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryViewModel"/> class.
        /// </summary>
        public DirectoryViewModel(DirectoryInfo directoryInfo, IIconService iconService, IFileSystemService fileSystemService, IEventAggregator eventAggregator)
            : base(iconService, fileSystemService)
        {
            _directoryInfo = directoryInfo;
            _eventAggregator = eventAggregator;
            Name = _directoryInfo.Name;
            Path = _directoryInfo.FullName;
            _imageSource = IconService.GetIcon(Path, ItemType.Folder, IconSize.Small, ItemState.Close);
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

        public override async void Explore()
        {
            if (Children.Count == 1 && Children[0] is DummyViewModel)
            {
                Children.Clear();

                var directories = await FileSystemService.GetDirectoriesAsync(Path);
                foreach (var dir in directories)
                {
                    var dirViewModel = new DirectoryViewModel(dir, IconService, FileSystemService, _eventAggregator);
                    dirViewModel.PropertyChanged += OnChildPropertyChanged;
                    Children.Add(dirViewModel);
                }

                var files = await FileSystemService.GetFilesAsync(Path);
                foreach (var file in files)
                {
                    var fileViewModel = new FileViewModel(file, IconService, FileSystemService);
                    fileViewModel.PropertyChanged += OnChildPropertyChanged;
                    Children.Add(fileViewModel);
                }

                ImageSource = IconService.GetIcon(Path, ItemType.Folder, IconSize.Small, ItemState.Open);

                // Pubblica l'evento dopo aver esplorato
                _eventAggregator.Publish(new AfterExploreEvent(Path));
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
