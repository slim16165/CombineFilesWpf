// FileSystemObjectInfo.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using TreeViewFileExplorer.Enums;

namespace TreeViewFileExplorer.ShellClasses;

public class FileSystemObjectInfo : BaseObject, IFileSystemObjectInfo
{
    public FileSystemObjectInfo(FileSystemInfo info)
    {
        if (this is DummyFileSystemObjectInfo)
        {
            return;
        }

        Children = [];
        FileSystemInfo = info;

        if (info is DirectoryInfo)
        {
            ImageSource = FolderManager.GetImageSource(info.FullName, ItemState.Close);
            AddDummy();
        }
        else if (info is FileInfo)
        {
            ImageSource = FileManager.GetImageSource(info.FullName);
        }

        PropertyChanged += new PropertyChangedEventHandler(FileSystemObjectInfo_PropertyChanged);
    }

    public FileSystemObjectInfo(DriveInfo drive)
        : this(drive.RootDirectory)
    {
        Drive = drive; // Imposta la proprietà Drive
    }

    #region Events

    public event EventHandler BeforeExpand;

    public event EventHandler AfterExpand;

    public event EventHandler BeforeExplore;

    public event EventHandler AfterExplore;

    private void RaiseBeforeExpand()
    {
        BeforeExpand?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseAfterExpand()
    {
        AfterExpand?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseBeforeExplore()
    {
        BeforeExplore?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseAfterExplore()
    {
        AfterExplore?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region EventHandlers

    void FileSystemObjectInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (FileSystemInfo is DirectoryInfo)
        {
            if (string.Equals(e.PropertyName, "IsExpanded", StringComparison.CurrentCultureIgnoreCase))
            {
                RaiseBeforeExpand();
                if (IsExpanded)
                {
                    ImageSource = FolderManager.GetImageSource(FileSystemInfo.FullName, ItemState.Open);
                    if (HasDummy())
                    {
                        RaiseBeforeExplore();
                        RemoveDummy();
                        ExploreDirectories();
                        ExploreFiles();
                        RaiseAfterExplore();
                    }
                }
                else
                {
                    ImageSource = FolderManager.GetImageSource(FileSystemInfo.FullName, ItemState.Close);
                }
                RaiseAfterExpand();
            }
        }
    }

    #endregion

    #region Properties

    public ObservableCollection<IFileSystemObjectInfo> Children
    {
        get => GetValue<ObservableCollection<IFileSystemObjectInfo>>("Children");
        private set => SetValue("Children", value);
    }

    public ImageSource ImageSource
    {
        get => GetValue<ImageSource>("ImageSource");
        private set => SetValue("ImageSource", value);
    }

    public bool IsSelected
    {
        get => GetValue<bool>("IsSelected");
        set => SetValue("IsSelected", value);
    }

    public bool IsExpanded
    {
        get => GetValue<bool>("IsExpanded");
        set => SetValue("IsExpanded", value);
    }

    public FileSystemInfo FileSystemInfo
    {
        get => GetValue<FileSystemInfo>("FileSystemInfo");
        private set => SetValue("FileSystemInfo", value);
    }

    private DriveInfo Drive
    {
        get => GetValue<DriveInfo>("Drive");
        set => SetValue("Drive", value);
    }

    #endregion

    #region Methods

    private void AddDummy()
    {
        Children.Add(new DummyFileSystemObjectInfo());
    }

    internal bool HasDummy()
    {
        return GetDummy() != null;
    }

    private DummyFileSystemObjectInfo GetDummy()
    {
        return Children.OfType<DummyFileSystemObjectInfo>().FirstOrDefault();
    }

    internal void RemoveDummy()
    {
        Children.Remove(GetDummy());
    }






    // FileSystemObjectInfo.cs (modifiche ai metodi ExploreDirectories e ExploreFiles)

    internal void ExploreDirectories()
    {
        if (Drive?.IsReady == false)
        {
            return;
        }
        if (FileSystemInfo is DirectoryInfo)
        {
            DirectoryInfo dir = (DirectoryInfo)FileSystemInfo;
            DirectoryInfo[] directories;
            try
            {
                directories = dir.GetDirectories();
            }
            catch (UnauthorizedAccessException)
            {
                directories = new DirectoryInfo[0];
            }
            catch (Exception)
            {
                directories = new DirectoryInfo[0];
            }

            foreach (var directory in directories.OrderBy(d => d.Name))
            {
                if ((directory.Attributes & FileAttributes.System) != FileAttributes.System &&
                    (directory.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                {
                    var fileSystemObject = new FileSystemObjectInfo(directory);
                    fileSystemObject.BeforeExplore += FileSystemObject_BeforeExplore;
                    fileSystemObject.AfterExplore += FileSystemObject_AfterExplore;
                    Children.Add(fileSystemObject);
                }
            }
        }
    }

    internal void ExploreFiles()
    {
        if (Drive?.IsReady == false)
        {
            return;
        }
        if (FileSystemInfo is DirectoryInfo)
        {
            DirectoryInfo dir = (DirectoryInfo)FileSystemInfo;
            FileInfo[] files;
            try
            {
                files = dir.GetFiles();
            }
            catch (UnauthorizedAccessException)
            {
                files = new FileInfo[0];
            }
            catch (Exception)
            {
                files = new FileInfo[0];
            }

            foreach (var file in files.OrderBy(d => d.Name))
            {
                if ((file.Attributes & FileAttributes.System) != FileAttributes.System &&
                    (file.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                {
                    Children.Add(new FileSystemObjectInfo(file));
                }
            }
        }
    }

    private void FileSystemObject_AfterExplore(object sender, EventArgs e)
    {
        RaiseAfterExplore();
    }

    private void FileSystemObject_BeforeExplore(object sender, EventArgs e)
    {
        RaiseBeforeExplore();
    }

    #endregion
}