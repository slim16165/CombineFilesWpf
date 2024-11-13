using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TreeViewFileExplorer.Model;

namespace CombineFilesWpf;

public partial class MainWindow : Window
{
    private CancellationTokenSource cts;

    public MainWindow()
    {
        InitializeComponent();
    }

    // Metodo per aggiungere cartelle
    private void BtnAddFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true // per la selezione di cartelle
        };

        var folderDialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog
        {
            IsFolderPicker = true
        };

        if (folderDialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
        {
            AddFilesFromFolder(folderDialog.FileName);
        }
    }

    // Metodo per aggiungere file
    private void BtnAddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleziona i file da includere",
            Multiselect = true
        };
        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                if (ShouldIncludeFile(file))
                {
                    var fileItem = new FileItem
                    {
                        IsSelected = true,
                        Path = file,
                        Name = System.IO.Path.GetFileName(file),
                        IsFolder = false
                    };
                    FileList.AddFile(fileItem);
                }
            }
        }
    }

    // Metodo per aggiungere file dalla cartella selezionata
    private void AddFilesFromFolder(string folderPath)
    {
        var searchOption = FilterOptions.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        try
        {
            var files = Directory.GetFiles(folderPath, "*.*", searchOption);
            foreach (var file in files)
            {
                if (ShouldIncludeFile(file))
                {
                    var fileItem = new FileItem
                    {
                        IsSelected = true,
                        Path = file,
                        Name = System.IO.Path.GetFileName(file),
                        IsFolder = false
                    };
                    FileList.AddFile(fileItem);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore nell'accesso alla cartella {folderPath}: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Metodo per determinare se un file deve essere incluso
    private bool ShouldIncludeFile(string filePath)
    {
        // Escludi file nascosti
        if (FilterOptions.ExcludeHidden && (new FileInfo(filePath).Attributes.HasFlag(FileAttributes.Hidden)))
        {
            return false;
        }

        // Escludi percorsi specifici
        if (!string.IsNullOrWhiteSpace(FilterOptions.ExcludePaths))
        {
            var excludePaths = FilterOptions.ExcludePaths.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var exclude in excludePaths)
            {
                if (filePath.IndexOf(exclude, StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
            }
        }

        string extension = System.IO.Path.GetExtension(filePath).ToLower();

        // Escludi estensioni specifiche
        if (!string.IsNullOrWhiteSpace(FilterOptions.ExcludeExtensions))
        {
            var excludeExtensions = FilterOptions.ExcludeExtensions.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);
            if (Array.Exists(excludeExtensions, e => e.Equals(extension, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        // Includi solo estensioni specifiche
        if (!string.IsNullOrWhiteSpace(FilterOptions.IncludeExtensions))
        {
            var includeExtensions = FilterOptions.IncludeExtensions.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);
            if (!Array.Exists(includeExtensions, e => e.Equals(extension, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        return true;
    }

    // Pulsante per rimuovere i file selezionati
    private void BtnRemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        foreach (var file in FileList.SelectedFiles)
        {
            FileList.SelectedFiles.Remove(file);
        }
    }

    // Pulsante per svuotare la lista
    private void BtnClearList_Click(object sender, RoutedEventArgs e)
    {
        FileList.ClearFiles();
    }

    // Pulsante per avviare il merging
    private async void BtnStartMerging_Click(object sender, RoutedEventArgs e)
    {
        var filesToMerge = new List<FileItem>(FileList.SelectedFiles);
        if (filesToMerge.Count == 0)
        {
            MessageBox.Show("Nessun file selezionato per il merging.", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string outputFolder = OutputOptions.OutputFolder;
        if (string.IsNullOrWhiteSpace(outputFolder) || !Directory.Exists(outputFolder))
        {
            MessageBox.Show("Percorso di output non valido.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        string outputFileName = OutputOptions.OutputFileName;
        if (string.IsNullOrWhiteSpace(outputFileName))
        {
            MessageBox.Show("Nome del file di output non valido.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Disabilita i controlli durante il merging
        ToggleControls(false);
        cts = new CancellationTokenSource();
        progressBar.Value = 0;
        progressBar.Maximum = filesToMerge.Count;
        lblProgress.Content = $"0 / {filesToMerge.Count}";

        try
        {
            await Task.Run(() => StartMerging(filesToMerge, outputFolder, outputFileName, cts.Token));
            MessageBox.Show("Merging completato con successo.", "Successo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("Merging interrotto dall'utente.", "Interrotto", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore durante il merging: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Riabilita i controlli
            ToggleControls(true);
        }
    }

    // Metodo per eseguire il merging
    private void StartMerging(List<FileItem> filesToMerge, string outputFolder, string outputFileName, CancellationToken token)
    {
        // Implementazione del merging come nel codice originale
        // ...
    }

    // Metodo per disabilitare/abilitare i controlli
    private void ToggleControls(bool isEnabled)
    {
        Dispatcher.Invoke(() =>
        {
            BtnStartMerging.IsEnabled = isEnabled;
            BtnStopMerging.IsEnabled = !isEnabled;
            BtnAddFolder.IsEnabled = isEnabled;
            BtnAddFiles.IsEnabled = isEnabled;
            BtnRemoveSelected.IsEnabled = isEnabled;
            BtnClearList.IsEnabled = isEnabled;

            FilterOptions.ToggleControls(isEnabled);
            OutputOptions.ToggleControls(isEnabled);
            FileList.ToggleControls(isEnabled);
        });
    }

    // Pulsante per interrompere il merging
    private void BtnStopMerging_Click(object sender, RoutedEventArgs e)
    {
        cts?.Cancel();
    }

    // Pulsante per salvare la configurazione
    private void BtnSaveConfig_Click(object sender, RoutedEventArgs e)
    {
        var config = new MergingConfig
        {
            IncludeSubfolders = FilterOptions.IncludeSubfolders,
            ExcludeHidden = FilterOptions.ExcludeHidden,
            IncludeExtensions = FilterOptions.IncludeExtensions,
            ExcludeExtensions = FilterOptions.ExcludeExtensions,
            ExcludePaths = FilterOptions.ExcludePaths,
            OutputFolder = OutputOptions.OutputFolder,
            OutputFileName = OutputOptions.OutputFileName,
            OneFilePerExtension = OutputOptions.OneFilePerExtension,
            OverwriteFiles = OutputOptions.OverwriteFiles
        };

        var saveFileDialog = new SaveFileDialog
        {
            Title = "Salva Configurazione",
            Filter = "JSON Files (*.json)|*.json",
            DefaultExt = ".json"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(saveFileDialog.FileName, json);
                MessageBox.Show("Configurazione salvata con successo.", "Successo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante il salvataggio della configurazione: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Pulsante per caricare la configurazione
    private void BtnLoadConfig_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Carica Configurazione",
            Filter = "JSON Files (*.json)|*.json",
            DefaultExt = ".json"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                var json = File.ReadAllText(openFileDialog.FileName);
                var config = JsonConvert.DeserializeObject<MergingConfig>(json);

                FilterOptions.chkIncludeSubfolders.IsChecked = config.IncludeSubfolders;
                FilterOptions.chkExcludeHidden.IsChecked = config.ExcludeHidden;
                FilterOptions.txtIncludeExtensions.Text = config.IncludeExtensions;
                FilterOptions.txtExcludeExtensions.Text = config.ExcludeExtensions;
                FilterOptions.txtExcludePaths.Text = config.ExcludePaths;
                OutputOptions.txtOutputFolder.Text = config.OutputFolder;
                OutputOptions.txtOutputFileName.Text = config.OutputFileName;
                OutputOptions.chkOneFilePerExtension.IsChecked = config.OneFilePerExtension;
                OutputOptions.chkOverwriteFiles.IsChecked = config.OverwriteFiles;

                MessageBox.Show("Configurazione caricata con successo.", "Successo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante il caricamento della configurazione: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}