using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TreeViewFileExplorer.ViewModels
{
    public interface IFileSystemObjectViewModel
    {
        ObservableCollection<IFileSystemObjectViewModel> Children { get; }
        ImageSource ImageSource { get; }
        string Name { get; }
        string Path { get; }
        bool IsSelected { get; set; }
        bool IsExpanded { get; set; }
        Task ExploreAsync();
    }
}