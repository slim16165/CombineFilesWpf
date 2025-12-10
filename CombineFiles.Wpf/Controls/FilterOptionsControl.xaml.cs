using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace CombineFilesWpf.Controls;

public partial class FilterOptionsControl : UserControl
{
    public FilterOptionsControl()
    {
        InitializeComponent();
    }

    // Proprietà per accedere ai controlli dal MainWindow
    public bool IncludeSubfolders => chkIncludeSubfolders.IsChecked == true;
    public bool ExcludeHidden => chkExcludeHidden.IsChecked == true;
    public string IncludeExtensions => txtIncludeExtensions.Text;
    public string ExcludeExtensions => txtExcludeExtensions.Text;
    public string IncludePaths => txtIncludePaths.Text;
    public string ExcludePaths => txtExcludePaths.Text;

    // Metodo per disabilitare/abilitare i controlli
    public void ToggleControls(bool isEnabled)
    {
        chkIncludeSubfolders.IsEnabled = isEnabled;
        chkExcludeHidden.IsEnabled = isEnabled;
        txtIncludeExtensions.IsEnabled = isEnabled;
        txtExcludeExtensions.IsEnabled = isEnabled;
        txtIncludePaths.IsEnabled = isEnabled;
        txtExcludePaths.IsEnabled = isEnabled;
        btnSelectIncludePaths.IsEnabled = isEnabled;
        btnSelectExcludePaths.IsEnabled = isEnabled;
    }

    private void BtnSelectIncludePaths_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true,
            Multiselect = true,
            Title = "Seleziona cartelle da includere"
        };

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            var paths = string.Join(";", dialog.FileNames);
            txtIncludePaths.Text = paths;
        }
    }

    private void BtnSelectExcludePaths_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true,
            Multiselect = true,
            Title = "Seleziona cartelle da escludere"
        };

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            var paths = string.Join(";", dialog.FileNames);
            txtExcludePaths.Text = paths;
        }
    }
}