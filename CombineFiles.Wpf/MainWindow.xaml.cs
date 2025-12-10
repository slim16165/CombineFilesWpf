using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CombineFiles.Core;
using CombineFiles.Core.Configuration;
using CombineFiles.Core.Helpers;
using TreeViewFileExplorer.Model;

namespace CombineFilesWpf;

public partial class MainWindow : Window
{
    private CancellationTokenSource cts;

    public MainWindow()
    {
        InitializeComponent();
    }

    // Metodo per aggiungere cartelle (UI)
    private void BtnAddFolder_Click(object sender, RoutedEventArgs e)
    {
        var folderDialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true
        };

        if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            AddFilesFromFolder(folderDialog.FileName);
        }
    }

    // Metodo per aggiungere file (UI)
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
                // Usa il helper per verificare se includere il file
                if (FileFilterHelper.ShouldIncludeFile(
                        file,
                        FilterOptions.ExcludeHidden,
                        FilterOptions.ExcludePaths,
                        FilterOptions.ExcludeExtensions,
                        FilterOptions.IncludeExtensions))
                {
                    var fileItem = new FileItem
                    {
                        IsSelected = true,
                        Path = file,
                        Name = Path.GetFileName(file),
                        IsFolder = false
                    };
                    FileList.AddFile(fileItem);
                }
            }
        }
    }

    // Metodo per aggiungere file dalla cartella selezionata (UI)
    private void AddFilesFromFolder(string folderPath)
    {
        var searchOption = FilterOptions.IncludeSubfolders
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;
        try
        {
            var files = Directory.GetFiles(folderPath, "*.*", searchOption);
            foreach (var file in files)
            {
                if (FileFilterHelper.ShouldIncludeFile(
                        file,
                        FilterOptions.ExcludeHidden,
                        FilterOptions.ExcludePaths,
                        FilterOptions.ExcludeExtensions,
                        FilterOptions.IncludeExtensions))
                {
                    var fileItem = new FileItem
                    {
                        IsSelected = true,
                        Path = file,
                        Name = Path.GetFileName(file),
                        IsFolder = false
                    };
                    FileList.AddFile(fileItem);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore nell'accesso alla cartella {folderPath}: {ex.Message}", "Errore",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Pulsante per rimuovere i file selezionati (UI)
    private void BtnRemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        foreach (var file in FileList.SelectedFiles)
        {
            FileList.SelectedFiles.Remove(file);
        }
    }

    // Pulsante per svuotare la lista (UI)
    private void BtnClearList_Click(object sender, RoutedEventArgs e)
    {
        FileList.ClearFiles();
    }

    // Pulsante per avviare il merging (UI)
    private async void BtnStartMerging_Click(object sender, RoutedEventArgs e)
    {
        var filesToMerge = new List<FileItem>(FileList.SelectedFiles);
        if (filesToMerge.Count == 0)
        {
            MessageBox.Show("Nessun file selezionato per il merging.", "Attenzione", MessageBoxButton.OK,
                MessageBoxImage.Warning);
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
            MessageBox.Show("Nome del file di output non valido.", "Errore", MessageBoxButton.OK,
                MessageBoxImage.Error);
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
            MessageBox.Show("Merging completato con successo.", "Successo", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("Merging interrotto dall'utente.", "Interrotto", MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore durante il merging: {ex.Message}", "Errore", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            // Riabilita i controlli
            ToggleControls(true);
        }
    }

    // Metodo per eseguire il merging (da implementare secondo la logica specifica)
    private void StartMerging(List<FileItem> filesToMerge, string outputFolder, string outputFileName,
        CancellationToken token)
    {
        // Implementazione del merging...
    }

    // Metodo per disabilitare/abilitare i controlli (UI)
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

    // Pulsante per interrompere il merging (UI)
    private void BtnStopMerging_Click(object sender, RoutedEventArgs e)
    {
        cts?.Cancel();
    }

    // Pulsante per salvare la configurazione (UI)
    private void BtnSaveConfig_Click(object sender, RoutedEventArgs e)
    {
        // Crea l'oggetto per la configurazione di ricerca
        var searchConfig = new FileSearchConfig
        {
            IncludeSubfolders = FilterOptions.IncludeSubfolders,
            ExcludeHidden = FilterOptions.ExcludeHidden,
            IncludeExtensions = FilterOptions.IncludeExtensions,
            ExcludeExtensions = FilterOptions.ExcludeExtensions,
            IncludePaths = FilterOptions.IncludePaths,
            ExcludePaths = FilterOptions.ExcludePaths
        };

        // Crea l'oggetto per la configurazione di unione
        var mergeConfig = new FileMergeConfig
        {
            OutputFolder = OutputOptions.OutputFolder,
            OutputFileName = OutputOptions.OutputFileName,
            OneFilePerExtension = OutputOptions.OneFilePerExtension,
            OverwriteFiles = OutputOptions.OverwriteFiles
        };

        // Contenitore temporaneo per entrambe le configurazioni
        var configContainer = new
        {
            SearchConfig = searchConfig,
            MergeConfig = mergeConfig
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
                var json = JsonConvert.SerializeObject(configContainer, Formatting.Indented);
                File.WriteAllText(saveFileDialog.FileName, json);
                MessageBox.Show("Configurazione salvata con successo.", "Successo", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante il salvataggio della configurazione: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

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
                var configContainer = JsonConvert.DeserializeAnonymousType(json, new
                {
                    SearchConfig = new FileSearchConfig(),
                    MergeConfig = new FileMergeConfig()
                });

                // Aggiorna i campi di FilterOptions con i valori di SearchConfig
                FilterOptions.chkIncludeSubfolders.IsChecked = configContainer.SearchConfig.IncludeSubfolders;
                FilterOptions.chkExcludeHidden.IsChecked = configContainer.SearchConfig.ExcludeHidden;
                FilterOptions.txtIncludeExtensions.Text = configContainer.SearchConfig.IncludeExtensions;
                FilterOptions.txtExcludeExtensions.Text = configContainer.SearchConfig.ExcludeExtensions;
                FilterOptions.txtIncludePaths.Text = configContainer.SearchConfig.IncludePaths ?? "";
                FilterOptions.txtExcludePaths.Text = configContainer.SearchConfig.ExcludePaths;

                // Aggiorna i campi di OutputOptions con i valori di MergeConfig
                OutputOptions.txtOutputFolder.Text = configContainer.MergeConfig.OutputFolder;
                OutputOptions.txtOutputFileName.Text = configContainer.MergeConfig.OutputFileName;
                OutputOptions.chkOneFilePerExtension.IsChecked = configContainer.MergeConfig.OneFilePerExtension;
                OutputOptions.chkOverwriteFiles.IsChecked = configContainer.MergeConfig.OverwriteFiles;

                MessageBox.Show("Configurazione caricata con successo.", "Successo", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante il caricamento della configurazione: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}