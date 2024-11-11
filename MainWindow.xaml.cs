using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace FileMergerApp
{
    public partial class MainWindow : Window
    {
        private List<FileItem> selectedFiles = new List<FileItem>();
        private CancellationTokenSource cts;

        public MainWindow()
        {
            InitializeComponent();
            dataGridFiles.ItemsSource = selectedFiles;
            txtOutputFolder.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        // Metodo per aggiungere cartelle
        private void BtnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                AddFilesFromFolder(dialog.FileName);
            }
        }

        // Metodo per aggiungere file dalla cartella selezionata
        private void AddFilesFromFolder(string folderPath)
        {
            SearchOption searchOption = chkIncludeSubfolders.IsChecked == true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            try
            {
                var files = Directory.GetFiles(folderPath, "*.*", searchOption);
                foreach (var file in files)
                {
                    if (ShouldIncludeFile(file))
                    {
                        selectedFiles.Add(new FileItem { IsSelected = true, Path = file });
                    }
                }
                dataGridFiles.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nell'accesso alla cartella {folderPath}: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Metodo per aggiungere file individuali
        private void BtnAddFiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Seleziona i file da includere";
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    if (ShouldIncludeFile(file))
                    {
                        selectedFiles.Add(new FileItem { IsSelected = true, Path = file });
                    }
                }
                dataGridFiles.Items.Refresh();
            }
        }

        // Metodo per determinare se un file deve essere incluso
        private bool ShouldIncludeFile(string filePath)
        {
            // Escludi file nascosti
            if (chkExcludeHidden.IsChecked == true && (new FileInfo(filePath).Attributes.HasFlag(FileAttributes.Hidden)))
            {
                return false;
            }

            // Escludi percorsi specifici
            if (!string.IsNullOrWhiteSpace(txtExcludePaths.Text))
            {
                var excludePaths = txtExcludePaths.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var exclude in excludePaths)
                {
                    if (filePath.IndexOf(exclude.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                        return false;
                }
            }

            string extension = System.IO.Path.GetExtension(filePath).ToLower();

            // Escludi estensioni specifiche
            if (!string.IsNullOrWhiteSpace(txtExcludeExtensions.Text))
            {
                var excludeExtensions = txtExcludeExtensions.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var ext in excludeExtensions)
                {
                    string cleanExt = ext.StartsWith(".") ? ext.ToLower() : "." + ext.ToLower();
                    if (extension == cleanExt)
                        return false;
                }
            }

            // Includi solo estensioni specifiche
            if (!string.IsNullOrWhiteSpace(txtIncludeExtensions.Text))
            {
                var includeExtensions = txtIncludeExtensions.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var ext in includeExtensions)
                {
                    string cleanExt = ext.StartsWith(".") ? ext.ToLower() : "." + ext.ToLower();
                    if (extension == cleanExt)
                        return true;
                }
                return false;
            }

            return true;
        }

        // Pulsante per rimuovere i file selezionati
        private void BtnRemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            selectedFiles.RemoveAll(f => f.IsSelected);
            dataGridFiles.Items.Refresh();
        }

        // Pulsante per svuotare la lista
        private void BtnClearList_Click(object sender, RoutedEventArgs e)
        {
            selectedFiles.Clear();
            dataGridFiles.Items.Refresh();
        }

        // Pulsante per sfogliare la cartella di output
        private void BtnBrowseOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtOutputFolder.Text = dialog.FileName;
            }
        }

        // Pulsante per avviare il merging
        private async void BtnStartMerging_Click(object sender, RoutedEventArgs e)
        {
            if (selectedFiles.Count == 0)
            {
                MessageBox.Show("Nessun file selezionato per il merging.", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string outputFolder = txtOutputFolder.Text;
            if (string.IsNullOrWhiteSpace(outputFolder) || !Directory.Exists(outputFolder))
            {
                MessageBox.Show("Percorso di output non valido.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Disabilita i controlli durante il merging
            ToggleControls(false);
            cts = new CancellationTokenSource();
            progressBar.Value = 0;
            progressBar.Maximum = selectedFiles.Count;
            lblProgress.Content = $"0 / {selectedFiles.Count}";

            try
            {
                await Task.Run(() => StartMerging(cts.Token));
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
        private void StartMerging(CancellationToken token)
        {
            Dictionary<string, List<FileItem>> filesByExtension = new Dictionary<string, List<FileItem>>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in selectedFiles)
            {
                token.ThrowIfCancellationRequested();
                if (file.IsSelected)
                {
                    string extension = System.IO.Path.GetExtension(file.Path).ToLower();
                    if (!filesByExtension.ContainsKey(extension))
                    {
                        filesByExtension[extension] = new List<FileItem>();
                    }
                    filesByExtension[extension].Add(file);
                }
            }

            int processedFiles = 0;

            if (chkOneFilePerExtension.IsChecked == true)
            {
                foreach (var kvp in filesByExtension)
                {
                    token.ThrowIfCancellationRequested();
                    string extension = kvp.Key;
                    string outputFileName = txtOutputFileName.Text.Replace("<extension>", extension.TrimStart('.'));
                    string outputFilePath = System.IO.Path.Combine(txtOutputFolder.Text, outputFileName);

                    if (File.Exists(outputFilePath) && chkOverwriteFiles.IsChecked != true)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"Il file {outputFilePath} esiste già e l'opzione per sovrascrivere non è selezionata.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                        return;
                    }

                    using (var writer = new StreamWriter(outputFilePath, chkOverwriteFiles.IsChecked == true ? false : true))
                    {
                        foreach (var fileItem in kvp.Value)
                        {
                            token.ThrowIfCancellationRequested();
                            string content = File.ReadAllText(fileItem.Path);

                            // Aggiungi commenti in base all'estensione
                            string commentStart = GetCommentStartTypeForLanguage(extension.TrimStart('.'));
                            string commentEnd = GetCommentEndTypeForLanguage(extension.TrimStart('.'));

                            writer.WriteLine($"{commentStart} Inizio di {System.IO.Path.GetFileName(fileItem.Path)} {commentEnd}");
                            writer.WriteLine(content);
                            writer.WriteLine($"{commentStart} Fine di {System.IO.Path.GetFileName(fileItem.Path)} {commentEnd}");
                            writer.WriteLine();

                            processedFiles++;
                            Dispatcher.Invoke(() =>
                            {
                                progressBar.Value = processedFiles;
                                lblProgress.Content = $"{processedFiles} / {selectedFiles.Count}";
                            });
                        }
                    }
                }
            }
            else
            {
                string outputFileName = txtOutputFileName.Text.Replace("<extension>", "merged");
                string outputFilePath = System.IO.Path.Combine(txtOutputFolder.Text, outputFileName);

                if (File.Exists(outputFilePath) && chkOverwriteFiles.IsChecked != true)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Il file {outputFilePath} esiste già e l'opzione per sovrascrivere non è selezionata.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    return;
                }

                using (var writer = new StreamWriter(outputFilePath, chkOverwriteFiles.IsChecked == true ? false : true))
                {
                    foreach (var fileItem in selectedFiles)
                    {
                        token.ThrowIfCancellationRequested();
                        if (fileItem.IsSelected)
                        {
                            string content = File.ReadAllText(fileItem.Path);
                            string extension = System.IO.Path.GetExtension(fileItem.Path).TrimStart('.');

                            // Aggiungi commenti in base all'estensione
                            string commentStart = GetCommentStartTypeForLanguage(extension);
                            string commentEnd = GetCommentEndTypeForLanguage(extension);

                            writer.WriteLine($"{commentStart} Inizio di {System.IO.Path.GetFileName(fileItem.Path)} {commentEnd}");
                            writer.WriteLine(content);
                            writer.WriteLine($"{commentStart} Fine di {System.IO.Path.GetFileName(fileItem.Path)} {commentEnd}");
                            writer.WriteLine();

                            processedFiles++;
                            Dispatcher.Invoke(() =>
                            {
                                progressBar.Value = processedFiles;
                                lblProgress.Content = $"{processedFiles} / {selectedFiles.Count}";
                            });
                        }
                    }
                }
            }
        }

        // Metodi per ottenere i simboli di commento
        private string GetCommentStartTypeForLanguage(string extension)
        {
            switch (extension.ToLower())
            {
                case "cs":
                case "c":
                case "cpp":
                case "js":
                case "java":
                    return "//";
                case "html":
                case "xml":
                    return "<!--";
                case "sql":
                case "lua":
                    return "--";
                case "py":
                case "rb":
                    return "#";
                default:
                    return "//";
            }
        }

        private string GetCommentEndTypeForLanguage(string extension)
        {
            switch (extension.ToLower())
            {
                case "html":
                case "xml":
                    return "-->";
                default:
                    return "";
            }
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
                chkIncludeSubfolders.IsEnabled = isEnabled;
                chkExcludeHidden.IsEnabled = isEnabled;
                txtIncludeExtensions.IsEnabled = isEnabled;
                txtExcludeExtensions.IsEnabled = isEnabled;
                txtExcludePaths.IsEnabled = isEnabled;
                txtOutputFolder.IsEnabled = isEnabled;
                txtOutputFileName.IsEnabled = isEnabled;
                chkOneFilePerExtension.IsEnabled = isEnabled;
                chkOverwriteFiles.IsEnabled = isEnabled;
                dataGridFiles.IsEnabled = isEnabled;
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
            // Implementazione del salvataggio delle configurazioni
            // Potresti utilizzare un file JSON o XML per salvare le impostazioni
        }

        // Pulsante per caricare la configurazione
        private void BtnLoadConfig_Click(object sender, RoutedEventArgs e)
        {
            // Implementazione del caricamento delle configurazioni
            // Potresti utilizzare un file JSON o XML per caricare le impostazioni
        }
    }

    // Classe per rappresentare un file
    public class FileItem
    {
        public bool IsSelected { get; set; }
        public string Path { get; set; }
    }
}
