using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CombineFilesWpf.Controls;

public partial class OutputOptionsControl : UserControl
{
    public OutputOptionsControl()
    {
        InitializeComponent();
        txtOutputFolder.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    private void BtnBrowseOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CommonOpenFileDialog
        {
            Title = "Seleziona la cartella di output",
            IsFolderPicker = true
        };

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            txtOutputFolder.Text = dialog.FileName;
        }
    }

    // Proprietà per accedere ai controlli dal MainWindow
    public string OutputFolder => txtOutputFolder.Text;
    public string OutputFileName => txtOutputFileName.Text;
    public bool OneFilePerExtension => chkOneFilePerExtension.IsChecked == true;
    public bool OverwriteFiles => chkOverwriteFiles.IsChecked == true;

    // Metodo per disabilitare/abilitare i controlli
    public void ToggleControls(bool isEnabled)
    {
        txtOutputFolder.IsEnabled = isEnabled;
        txtOutputFileName.IsEnabled = isEnabled;
        chkOneFilePerExtension.IsEnabled = isEnabled;
        chkOverwriteFiles.IsEnabled = isEnabled;
    }
}