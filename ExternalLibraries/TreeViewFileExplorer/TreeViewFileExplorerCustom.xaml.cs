// TreeViewFileExplorerCustom.xaml.cs
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;
using TreeViewFileExplorer.Model;
using TreeViewFileExplorer.ShellClasses;

namespace TreeViewFileExplorer;

public partial class TreeViewFileExplorerCustom : UserControl
{
    public TreeViewFileExplorerCustom()
    {
        InitializeComponent();
        InitializeFileSystemObjects();
    }

    /// <summary>
    /// Espone gli elementi selezionati del RadTreeView interno.
    /// </summary>
    public IList SelectedItems => radTreeView.SelectedItems;

    public ObservableCollection<FileItem> SelectedFiles { get; private set; } = new ObservableCollection<FileItem>();


    #region Events

    private void FileSystemObject_AfterExplore(object sender, EventArgs e)
    {
        Cursor = Cursors.Arrow;
    }

    private void FileSystemObject_BeforeExplore(object sender, EventArgs e)
    {
        Cursor = Cursors.Wait;
    }

    private void RadTreeView_LoadOnDemand(object sender, Telerik.Windows.RadRoutedEventArgs e)
    {
        if (e.OriginalSource is RadTreeViewItem item && item.DataContext is FileSystemObjectInfo fso)
        {
            if (fso.HasDummy())
            {
                fso.RemoveDummy();
                fso.ExploreDirectories();
                fso.ExploreFiles();
            }
        }
    }

    private void RadTreeView_ItemPrepared(object sender, RadTreeViewItemPreparedEventArgs radTreeViewItemPreparedEventArgs)
    {
        // Eventuale logica aggiuntiva durante la preparazione degli elementi
    }

    #endregion

    #region Methods

    private void InitializeFileSystemObjects()
    {
        var drives = DriveInfo.GetDrives();
        foreach (var drive in drives)
        {
            var fileSystemObject = new FileSystemObjectInfo(drive);
            fileSystemObject.BeforeExplore += FileSystemObject_BeforeExplore;
            fileSystemObject.AfterExplore += FileSystemObject_AfterExplore;
            radTreeView.Items.Add(fileSystemObject);
        }
        PreSelect(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
    }

    private void PreSelect(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }
        var driveFileSystemObjectInfo = GetDriveFileSystemObjectInfo(path);
        if (driveFileSystemObjectInfo != null)
        {
            driveFileSystemObjectInfo.IsExpanded = true;
            PreSelect(driveFileSystemObjectInfo, path);
        }
    }

    private void PreSelect(FileSystemObjectInfo fileSystemObjectInfo, string path)
    {
        foreach (var childFileSystemObjectInfo in fileSystemObjectInfo.Children)
        {
            var isParentPath = IsParentPath(path, childFileSystemObjectInfo.FileSystemInfo.FullName);
            if (isParentPath)
            {
                if (string.Equals(childFileSystemObjectInfo.FileSystemInfo.FullName, path, StringComparison.OrdinalIgnoreCase))
                {
                    /* Elemento trovato per la preselezione */
                    // Seleziona l'elemento nel RadTreeView
                    var treeViewItem = radTreeView.ItemContainerGenerator.ContainerFromItem(childFileSystemObjectInfo) as RadTreeViewItem;
                    if (treeViewItem != null)
                    {
                        treeViewItem.IsSelected = true;
                        treeViewItem.BringIntoView();
                    }
                }
                else
                {
                    childFileSystemObjectInfo.IsExpanded = true;
                    PreSelect(childFileSystemObjectInfo, path);
                }
            }
        }
    }


    #endregion

    #region Helpers

    private FileSystemObjectInfo GetDriveFileSystemObjectInfo(string path)
    {
        var directory = new DirectoryInfo(path);
        var drive = DriveInfo
            .GetDrives()
            .FirstOrDefault(d => d.RootDirectory.FullName.Equals(directory.Root.FullName, StringComparison.OrdinalIgnoreCase));
        return GetDriveFileSystemObjectInfo(drive);
    }

    private FileSystemObjectInfo GetDriveFileSystemObjectInfo(DriveInfo drive)
    {
        foreach (var fso in radTreeView.Items.OfType<FileSystemObjectInfo>())
        {
            if (fso.FileSystemInfo.FullName.Equals(drive.RootDirectory.FullName, StringComparison.OrdinalIgnoreCase))
            {
                return fso;
            }
        }
        return null;
    }

    private bool IsParentPath(string path, string targetPath)
    {
        return path.StartsWith(targetPath, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    private void OnItemChecked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is FileSystemObjectInfo item)
        {
            UpdateSelectedFiles(item, true);
        }
    }

    private void OnItemUnchecked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is FileSystemObjectInfo item)
        {
            UpdateSelectedFiles(item, false);
        }
    }

    private static void UpdateSelectionState(FileSystemObjectInfo item, bool isSelected)
    {
        item.IsSelected = isSelected;

        // Seleziona o deseleziona ricorsivamente tutti i figli
        foreach (var child in item.Children)
        {
            UpdateSelectionState(child, isSelected);
        }
    }

    private void UpdateSelectedFiles(FileSystemObjectInfo item, bool isSelected)
    {
        if (isSelected)
        {
            if (item.FileSystemInfo is FileInfo fileInfo)
            {
                SelectedFiles.Add(new FileItem
                {
                    Name = fileInfo.Name,
                    Path = fileInfo.FullName,
                    IsFolder = false
                });
            }
            else if (item.FileSystemInfo is DirectoryInfo dirInfo)
            {
                SelectedFiles.Add(new FileItem
                {
                    Name = dirInfo.Name,
                    Path = dirInfo.FullName,
                    IsFolder = true
                });
            }
        }
        else
        {
            var existingItem = SelectedFiles.FirstOrDefault(f => f.Path.Equals(item.FileSystemInfo.FullName, StringComparison.OrdinalIgnoreCase));
            if (existingItem != null)
            {
                SelectedFiles.Remove(existingItem);
            }
        }

        // Sollevare un evento per notificare i cambiamenti
        OnSelectedFilesChanged();
    }

    // Evento per notificare i cambiamenti in SelectedFiles
    public event EventHandler SelectedFilesChanged;

    protected virtual void OnSelectedFilesChanged()
    {
        SelectedFilesChanged?.Invoke(this, EventArgs.Empty);
    }

    // Modificare i metodi OnItemChecked e OnItemUnchecked per aggiornare SelectedFiles
}