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
using TreeViewFileExplorer.ShellClasses;

namespace TreeViewFileExplorer
{
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
        public IList SelectedItems
        {
            get { return radTreeView.SelectedItems; }
        }

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
                        // Potresti aggiungere la logica per selezionare l'elemento qui
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
    }
}
