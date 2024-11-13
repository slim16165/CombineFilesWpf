using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;

namespace TreeViewFileExplorer.ShellClasses;

public interface IFileSystemObjectInfo
{
    ObservableCollection<IFileSystemObjectInfo> Children { get; }
    ImageSource ImageSource { get; }
    bool IsSelected { get; set; }
    bool IsExpanded { get; set; }
    FileSystemInfo FileSystemInfo { get; }
    void Explore();
}