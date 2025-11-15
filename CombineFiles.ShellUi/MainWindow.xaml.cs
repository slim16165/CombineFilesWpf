using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Infrastructure;
using CombineFiles.Core.Services;
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
            // Ottieni i file validi usando FileCollector (sostituisce FileProcessor)
            var logger = new Logger(enableLog: false);
            var fileCollector = new FileCollector(logger, new List<string>(), new List<string>(), new List<string>());
            var validFiles = GetFilesToProcess(args, fileCollector);
            
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

    /// <summary>
    /// Sostituisce FileProcessor.GetFilesToProcess - raccoglie file da percorsi specificati
    /// </summary>
    private static IEnumerable<string> GetFilesToProcess(string[] paths, FileCollector collector)
    {
        var validFiles = new List<string>();
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                var files = collector.GetAllFiles(path, recurse: true);
                validFiles.AddRange(files);
            }
            else if (File.Exists(path))
            {
                validFiles.Add(path);
            }
        }
        return validFiles;
    }

    private void FilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilesListBox.SelectedItem is not FileItem selectedItem) return;

        try
        {
            string content = File.ReadAllText(selectedItem.FilePath);
            // Esempio: se il file è JSON, prova a formattarlo
            

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
        string combined = CombineContents(files);

        // Copia negli appunti (usa System.Windows.Clipboard o Windows.Forms.Clipboard)
        System.Windows.Clipboard.SetText(combined);

        MessageBox.Show("Contenuto copiato negli appunti!", "CombineFilesApp", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// Sostituisce FileProcessor.CombineContents - combina contenuti file usando gli handler appropriati
    /// </summary>
    private static string CombineContents(IEnumerable<string> files)
    {
        var handlers = new Dictionary<string, CombineFiles.Core.Handlers.IFileContentHandler>(StringComparer.OrdinalIgnoreCase)
        {
            { ".csv", new CombineFiles.Core.Handlers.CsvContentHandler() },
            { ".json", new CombineFiles.Core.Handlers.JsonContentHandler() }
        };

        var sb = new System.Text.StringBuilder();
        foreach (var file in files)
        {
            sb.AppendLine($"### {Path.GetFileName(file)} ###");
            try
            {
                string content = File.ReadAllText(file);
                string extension = Path.GetExtension(file);

                if (!handlers.TryGetValue(extension, out var handler))
                {
                    handler = new CombineFiles.Core.Handlers.DefaultContentHandler();
                }
                sb.AppendLine(handler.Handle(content));
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[ERROR: unable to read {file} - {ex.Message}]");
            }
        }
        return sb.ToString();
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