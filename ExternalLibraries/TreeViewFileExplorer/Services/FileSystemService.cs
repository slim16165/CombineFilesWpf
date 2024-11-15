// FileSystemService.cs
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TreeViewFileExplorer.Services
{
    /// <summary>
    /// Service for interacting with the file system asynchronously.
    /// </summary>
    public class FileSystemService : IFileSystemService
    {
        public FileSystemService()
        {
        }

        public async Task<IEnumerable<DirectoryInfo>> GetDirectoriesAsync(string path, bool showHiddenFiles, Regex filterRegex)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var dir = new DirectoryInfo(path);
                    var directories = dir.GetDirectories();

                    if (!showHiddenFiles)
                    {
                        directories = directories.Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden)).ToArray();
                    }

                    if (filterRegex != null)
                    {
                        directories = directories.Where(d => filterRegex.IsMatch(d.Name)).ToArray();
                    }

                    return directories.OrderBy(d => d.Name);
                }
                catch
                {
                    return Enumerable.Empty<DirectoryInfo>();
                }
            });
        }

        public async Task<IEnumerable<FileInfo>> GetFilesAsync(string path, bool showHiddenFiles, Regex filterRegex)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var dir = new DirectoryInfo(path);
                    var files = dir.GetFiles();

                    if (!showHiddenFiles)
                    {
                        files = files.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden)).ToArray();
                    }

                    if (filterRegex != null)
                    {
                        files = files.Where(f => filterRegex.IsMatch(f.Name)).ToArray();
                    }

                    return files.OrderBy(f => f.Name);
                }
                catch
                {
                    return Enumerable.Empty<FileInfo>();
                }
            });
        }

        public async Task<bool> IsAccessibleAsync(string path)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var dir = new DirectoryInfo(path);
                    return dir.Exists && !dir.Attributes.HasFlag(FileAttributes.ReparsePoint);
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
