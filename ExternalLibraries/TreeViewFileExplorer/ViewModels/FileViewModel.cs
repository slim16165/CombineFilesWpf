// FileViewModel.cs
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using TreeViewFileExplorer.Enums;
using TreeViewFileExplorer.Services;

namespace TreeViewFileExplorer.ViewModels
{
    /// <summary>
    /// ViewModel for files.
    /// </summary>
    public class FileViewModel : BaseFileSystemObjectViewModel
    {
        private readonly FileInfo _fileInfo;
        private ImageSource _imageSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileViewModel"/> class.
        /// </summary>
        public FileViewModel(FileInfo fileInfo, IIconService iconService, IFileSystemService fileSystemService, bool showHiddenFiles, Regex filterRegex)
            : base(iconService, fileSystemService, showHiddenFiles, filterRegex)
        {
            _fileInfo = fileInfo;
            Name = _fileInfo.Name;
            Path = _fileInfo.FullName;
            _imageSource = IconService.GetIcon(Path, ItemType.File, IconSize.Small, ItemState.Undefined);
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

        public override Task ExploreAsync()
        {
            return Task.CompletedTask;
        }

    }
}