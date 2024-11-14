using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TreeViewFileExplorer.ViewModels
{
    public class DummyViewModel : IFileSystemObjectViewModel
    {
        public ObservableCollection<IFileSystemObjectViewModel> Children => new ObservableCollection<IFileSystemObjectViewModel>();
        public ImageSource ImageSource => null;
        public string Name => string.Empty;
        public string Path => string.Empty;
        public bool IsSelected { get; set; }
        public bool IsExpanded { get; set; }
        public Task ExploreAsync()
        {
            return default!;
        }
    }
}