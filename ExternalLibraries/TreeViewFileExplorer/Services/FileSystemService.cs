using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TreeViewFileExplorer.Services
{
    /// <summary>
    /// Service for interacting with the file system asynchronously.
    /// </summary>
    public class FileSystemService : IFileSystemService
    {
        /// <inheritdoc/>
        public async Task<IEnumerable<DirectoryInfo>> GetDirectoriesAsync(string path)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var dir = new DirectoryInfo(path);
                    return dir.GetDirectories()
                              .Where(d => !d.Attributes.HasFlag(FileAttributes.System | FileAttributes.Hidden))
                              .OrderBy(d => d.Name);
                }
                catch
                {
                    return Enumerable.Empty<DirectoryInfo>();
                }
            });
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<FileInfo>> GetFilesAsync(string path)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var dir = new DirectoryInfo(path);
                    return dir.GetFiles()
                              .Where(f => !f.Attributes.HasFlag(FileAttributes.System | FileAttributes.Hidden))
                              .OrderBy(f => f.Name);
                }
                catch
                {
                    return Enumerable.Empty<FileInfo>();
                }
            });
        }

        /// <inheritdoc/>
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
