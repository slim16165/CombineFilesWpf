using System;

namespace TreeViewFileExplorer.ShellClasses;

public class FileSystemEventHandler
{
    public void AttachEvents(IFileSystemObjectInfo fileSystemObject)
    {
        fileSystemObject.BeforeExplore += OnBeforeExplore;
        fileSystemObject.AfterExplore += OnAfterExplore;
    }

    private void OnBeforeExplore(object sender, EventArgs e)
    {
        // Handle before explore
    }

    private void OnAfterExplore(object sender, EventArgs e)
    {
        // Handle after explore
    }
}