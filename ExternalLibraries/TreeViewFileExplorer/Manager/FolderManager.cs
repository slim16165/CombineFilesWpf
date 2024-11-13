using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TreeViewFileExplorer.Enums;
using Size = System.Drawing.Size;

namespace TreeViewFileExplorer;

public static class FolderManager
{
    public static ImageSource GetImageSource(string directory, ItemState folderType)
    {
        return GetImageSource(directory, new Size(16, 16), folderType);
    }

    public static ImageSource GetImageSource(string directory, Size size, ItemState folderType)
    {
        using (var icon = ShellManager.GetIcon(directory, ItemType.Folder, IconSize.Large, folderType))
        {
            return Imaging.CreateBitmapSourceFromHIcon(icon.Handle,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(size.Width, size.Height));
        }
    }

    public static void ProcessMultipleFolders(List<string> directories)
    {
        if (directories == null || directories.Count == 0)
            return;

        // Esempio: Mostra tutti i percorsi delle cartelle selezionate
        string folderList = string.Join(Environment.NewLine, directories);
        MessageBox.Show($"Cartelle Selezionate:\n{folderList}", "Processo Cartelle");
    }

}