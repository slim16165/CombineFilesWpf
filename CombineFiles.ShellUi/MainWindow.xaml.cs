using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CombineFiles.Core;
using MessageBox = System.Windows.MessageBox;

namespace CombineFiles.ShellUi;

public partial class MainWindow : Window
{
    private List<FileItem> _fileItems = new List<FileItem>();

    public MainWindow()
    {
        InitializeComponent();

        // Leggi i parametri di riga di comando
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        if (args.Any())
        {
            // Ottieni i file validi (riprendendo la logica da FileProcessor)
            var validFiles = FileProcessor.GetFilesToProcess(args);
            foreach (var vf in validFiles)
            {
                _fileItems.Add(new FileItem { FilePath = vf, FileName = Path.GetFileName(vf) });
            }

            FilesListBox.ItemsSource = _fileItems;
        }
        else
        {
            MessageBox.Show("Nessun file selezionato in input.", "CombineFilesApp", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void FilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilesListBox.SelectedItem is not FileItem selectedItem) return;

        try
        {
            string content = File.ReadAllText(selectedItem.FilePath);
            // Esempio: se il file è JSON, prova a formattarlo
            if (selectedItem.FilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                content = FileProcessor.PrettyPrintJson(content);
            }
            else if (selectedItem.FilePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                content = FileProcessor.ConvertCsvToTable(content);
            }
            // Aggiungi altre formattazioni se necessario

            PreviewTextBox.Text = content;
        }
        catch (Exception ex)
        {
            PreviewTextBox.Text = $"Impossibile leggere il file: {ex.Message}";
        }
    }

    private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
    {
        // Esempio: unisce il contenuto di tutti i file e copia
        var files = _fileItems.Select(x => x.FilePath).ToList();
        string combined = FileProcessor.CombineContents(files);

        // Copia negli appunti (usa System.Windows.Clipboard o Windows.Forms.Clipboard)
        System.Windows.Clipboard.SetText(combined);

        MessageBox.Show("Contenuto copiato negli appunti!", "CombineFilesApp", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

/// <summary>
/// Classe di supporto per bindare file name e path in ListBox
/// </summary>
public class FileItem
{
    public string FileName { get; set; }
    public string FilePath { get; set; }
}