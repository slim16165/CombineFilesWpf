using System.Collections.Generic;

namespace TreeViewFileExplorer.Model;

public class FileItem
{
    public bool IsSelected { get; set; }
    public string Path { get; set; }
    public string Name { get; set; }
    public bool IsFolder { get; set; }
    public List<FileItem> Children { get; set; } = new List<FileItem>();
}