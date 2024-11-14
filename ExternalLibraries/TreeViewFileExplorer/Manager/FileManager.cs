using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TreeViewFileExplorer.Enums;
using TreeViewFileExplorer.Managers;
using Size = System.Drawing.Size;

namespace TreeViewFileExplorer.Manager;

public static class FileManager
{
    public static ImageSource GetImageSource(string filename)
    {
        return GetImageSource(filename, new Size(16, 16));
    }

    public static ImageSource GetImageSource(string filename, Size size)
    {
        using (var icon = ShellManager.GetIcon(Path.GetExtension(filename), ItemType.File, IconSize.Small, ItemState.Undefined))
        {
            return Imaging.CreateBitmapSourceFromHIcon(icon.Handle,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(size.Width, size.Height));
        }
    }

    public static void ProcessMultipleFiles(List<string> filenames)
    {
        if (filenames == null || filenames.Count == 0)
            return;

        // Esempio: Mostra tutti i percorsi dei file selezionati
        string fileList = string.Join(Environment.NewLine, filenames);
        MessageBox.Show($"File Selezionati:\n{fileList}", "Processo File");
    }
}