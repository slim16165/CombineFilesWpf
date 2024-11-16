using System;
using System.Drawing;
using System.Runtime.InteropServices;
using TreeViewFileExplorer.Enums;
using TreeViewFileExplorer.Structs;

namespace TreeViewFileExplorer.Manager;

/// <summary>
/// Provides methods to interact with the Windows Shell to retrieve icons.
/// </summary>
public class ShellManager
{
    /// <summary>
    /// Retrieves the icon associated with a file or directory.
    /// </summary>
    /// <param name="path">The path to the file or directory.</param>
    /// <param name="type">The type of the item (File or Folder).</param>
    /// <param name="iconSize">The desired icon size.</param>
    /// <param name="state">The state of the item (Open or Closed for folders).</param>
    /// <returns>An <see cref="Icon"/> representing the item's icon.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the icon cannot be retrieved.</exception>
    public Icon GetIcon(string path, ItemType type, IconSize iconSize, ItemState state)
    {
        ShellFileInfo fileInfo = default;
        try
        {
            uint attributes = (uint)(type == ItemType.Folder ? FileAttribute.Directory : FileAttribute.File);
            ShellAttribute flags = ShellAttribute.Icon | ShellAttribute.UseFileAttributes;

            if (type == ItemType.Folder && state == ItemState.Open)
            {
                flags |= ShellAttribute.OpenIcon;
            }

            flags |= iconSize == IconSize.Small ? ShellAttribute.SmallIcon : ShellAttribute.LargeIcon;

            fileInfo = new ShellFileInfo();
            uint size = (uint)Marshal.SizeOf(fileInfo);
            IntPtr result = NativeMethods.SHGetFileInfo(path, attributes, out fileInfo, size, flags);

            if (result == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to retrieve icon for path: {path}");
            }

            Icon icon = Icon.FromHandle(fileInfo.hIcon).Clone() as Icon;
            return icon;
        }
        catch (Exception ex)
        {
            //Logger.Error(ex, $"Exception occurred while retrieving icon for path: {path}");
            throw;
        }
        finally
        {

            NativeMethods.DestroyIcon(fileInfo.hIcon);
        }
    }

    private static class NativeMethods
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            out ShellFileInfo psfi,
            uint cbFileInfo,
            ShellAttribute uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);
    }
}