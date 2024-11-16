using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TreeViewFileExplorer.Services;

/// <summary>
/// Defines methods for accessing the file system asynchronously.
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// Asynchronously retrieves the directories at the specified path.
    /// </summary>
    Task<IEnumerable<DirectoryInfo>> GetDirectoriesAsync(string path, bool showHiddenFiles, Regex filterRegex);

    /// <summary>
    /// Asynchronously retrieves the files at the specified path.
    /// </summary>
    Task<IEnumerable<FileInfo>> GetFilesAsync(string path, bool showHiddenFiles, Regex filterRegex);

    /// <summary>
    /// Asynchronously checks if the specified path is accessible.
    /// </summary>
    Task<bool> IsAccessibleAsync(string path);
}