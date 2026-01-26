using System;
using System.Runtime.InteropServices;

namespace TreeViewFileExplorer.Structs;

/// <summary>
/// Struttura che contiene informazioni su un file o directory recuperate dal sistema operativo Windows.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct ShellFileInfo
{
    /// <summary>
    /// Handle dell'icona associata al file o directory.
    /// </summary>
    public IntPtr hIcon;

    /// <summary>
    /// Indice dell'icona nell'immagine list.
    /// </summary>
    public int iIcon;

    /// <summary>
    /// Attributi del file o directory.
    /// </summary>
    public uint dwAttributes;

    /// <summary>
    /// Nome visualizzato del file o directory.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string szDisplayName;

    /// <summary>
    /// Tipo del file (es. "Text Document", "Folder").
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
    public string szTypeName;

    /// <summary>
    /// Inizializza una nuova istanza di ShellFileInfo con valori di default sicuri.
    /// </summary>
    /// <remarks>
    /// Le stringhe vengono inizializzate a string.Empty per evitare problemi con il marshalling.
    /// Tuttavia, quando la struct viene passata a P/Invoke, il marshaller gestirà correttamente
    /// le stringhe anche se sono null.
    /// </remarks>
    public ShellFileInfo()
    {
        hIcon = IntPtr.Zero;
        iIcon = 0;
        dwAttributes = 0;
        szDisplayName = string.Empty;
        szTypeName = string.Empty;
    }
}