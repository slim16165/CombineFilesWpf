using System;
using System.Runtime.InteropServices;
using TreeViewFileExplorer.Structs;

namespace TreeViewFileExplorer;

/// <summary>
/// Wrapper per le chiamate P/Invoke al sistema operativo Windows.
/// </summary>
public static class Interop
{
    /// <summary>
    /// Recupera informazioni su un file o directory dal sistema operativo.
    /// </summary>
    /// <param name="path">Percorso del file o directory. Non può essere null o vuoto.</param>
    /// <param name="attributes">Attributi del file.</param>
    /// <param name="fileInfo">Struttura che riceverà le informazioni sul file.</param>
    /// <param name="size">Dimensione della struttura fileInfo.</param>
    /// <param name="flags">Flag che specificano quali informazioni recuperare.</param>
    /// <returns>Handle non-zero se l'operazione ha successo, altrimenti IntPtr.Zero.</returns>
    /// <exception cref="ArgumentNullException">Se path è null o vuoto.</exception>
    [DllImport("shell32.dll", CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern IntPtr SHGetFileInfo(
        [MarshalAs(UnmanagedType.LPTStr)] string path,
        uint attributes,
        out ShellFileInfo fileInfo,
        uint size,
        uint flags);

    /// <summary>
    /// Distrugge un'icona creata da CreateIcon, CreateIconIndirect, o LoadIcon.
    /// </summary>
    /// <param name="pointer">Handle dell'icona da distruggere. Può essere IntPtr.Zero (operazione no-op).</param>
    /// <returns>True se l'icona è stata distrutta con successo, altrimenti False.</returns>
    /// <remarks>
    /// È sicuro chiamare questo metodo con IntPtr.Zero - non farà nulla.
    /// </remarks>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(IntPtr pointer);
}