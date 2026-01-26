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
    /// <param name="path">The path to the file or directory. Cannot be null or empty.</param>
    /// <param name="type">The type of the item (File or Folder).</param>
    /// <param name="iconSize">The desired icon size.</param>
    /// <param name="state">The state of the item (Open or Closed for folders).</param>
    /// <returns>An <see cref="Icon"/> representing the item's icon.</returns>
    /// <exception cref="ArgumentException">Thrown when path is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the icon cannot be retrieved.</exception>
    public Icon GetIcon(string path, ItemType type, IconSize iconSize, ItemState state)
    {
        // Validazione input
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        }

        ShellFileInfo fileInfo = default;
        IntPtr iconHandle = IntPtr.Zero;

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
                int lastError = Marshal.GetLastWin32Error();
                throw new InvalidOperationException(
                    $"Failed to retrieve icon for path: {path}. Win32 error code: {lastError}");
            }

            // Verifica che l'handle dell'icona sia valido
            if (fileInfo.hIcon == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"SHGetFileInfo returned success but icon handle is invalid for path: {path}");
            }

            iconHandle = fileInfo.hIcon;
            Icon icon = Icon.FromHandle(iconHandle).Clone() as Icon;
            
            if (icon == null)
            {
                throw new InvalidOperationException(
                    $"Failed to create Icon from handle for path: {path}");
            }

            return icon;
        }
        catch (ArgumentException)
        {
            // Rilancia ArgumentException senza modifiche
            throw;
        }
        catch (InvalidOperationException)
        {
            // Rilancia InvalidOperationException senza modifiche
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Unexpected error occurred while retrieving icon for path: {path}", ex);
        }
        finally
        {
            // Distruggi l'icona solo se è stata creata e non è stata clonata
            // Nota: Icon.FromHandle crea un riferimento all'icona nativa, ma Clone() crea una copia
            // Quindi dobbiamo distruggere l'handle originale solo se non è stato clonato
            // In realtà, dopo Clone(), l'handle originale può essere distrutto
            if (iconHandle != IntPtr.Zero)
            {
                NativeMethods.DestroyIcon(iconHandle);
            }
        }
    }

    private static class NativeMethods
    {
        /// <summary>
        /// Recupera informazioni su un file o directory dal sistema operativo Windows.
        /// </summary>
        /// <param name="pszPath">Percorso del file o directory.</param>
        /// <param name="dwFileAttributes">Attributi del file.</param>
        /// <param name="psfi">Struttura che riceverà le informazioni sul file.</param>
        /// <param name="cbFileInfo">Dimensione della struttura psfi.</param>
        /// <param name="uFlags">Flag che specificano quali informazioni recuperare.</param>
        /// <returns>Handle non-zero se l'operazione ha successo, altrimenti IntPtr.Zero.</returns>
        [DllImport("shell32.dll", CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            out ShellFileInfo psfi,
            uint cbFileInfo,
            ShellAttribute uFlags);

        /// <summary>
        /// Distrugge un'icona creata da CreateIcon, CreateIconIndirect, o LoadIcon.
        /// </summary>
        /// <param name="hIcon">Handle dell'icona da distruggere. Può essere IntPtr.Zero (operazione no-op).</param>
        /// <returns>True se l'icona è stata distrutta con successo, altrimenti False.</returns>
        /// <remarks>
        /// È sicuro chiamare questo metodo con IntPtr.Zero - non farà nulla.
        /// </remarks>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr hIcon);
    }
}