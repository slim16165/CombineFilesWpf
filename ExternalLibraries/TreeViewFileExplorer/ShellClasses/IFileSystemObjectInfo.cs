using System;
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
    EventHandler<EventArgs> BeforeExplore { get; set; }
    void Explore();
    event EventHandler<EventArgs> AfterExplore;
}