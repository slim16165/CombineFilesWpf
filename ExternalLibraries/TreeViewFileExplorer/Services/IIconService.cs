using System.Windows.Media;
using TreeViewFileExplorer.Enums;

namespace TreeViewFileExplorer.Services;

/// <summary>
/// Defines methods for retrieving system icons.
/// </summary>
public interface IIconService
{
    /// <summary>
    /// Gets the icon for a file system item.
    /// </summary>
    ImageSource GetIcon(string path, ItemType type, IconSize size, ItemState state);
}