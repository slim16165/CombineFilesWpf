using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Controls;
using Newtonsoft.Json;
using Telerik.Windows.Controls;

namespace FileMergerApp
{
    public partial class MainWindow : Window
    {
        private List<FileItem> selectedFiles = new List<FileItem>();
        private CancellationTokenSource cts;
        private string[] commentStartByExtension = new string[]
        {
            "cs", "c", "cpp", "js", "java", // // comment
            "html", "xml", // <!-- comment -->
            "sql", "lua", // -- comment
            "py", "rb", // # comment
            // add more as needed
        };

        public MainWindow()
        {
            InitializeComponent();
            radTreeViewFiles.ItemsSource = new List<FileItem>();
            txtOutputFolder.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        // Metodo per aggiungere cartelle usando Telerik RadTreeView
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

        // Metodo per aggiungere file individuali
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
                        selectedFiles.Add(new FileItem { IsSelected = true, Path = file });
                    }
                }
                radTreeViewFiles.ItemsSource = selectedFiles;
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
                        selectedFiles.Add(new FileItem { IsSelected = true, Path = file, Name = System.IO.Path.GetFileName(file), Children = new List<FileItem>() });
                    }
                }

                // Aggiungi cartella come nodo principale
                var directoryInfo = new DirectoryInfo(folderPath);
                var rootItem = new FileItem
                {
                    IsSelected = true,
                    Path = folderPath,
                    Name = directoryInfo.Name,
                    IsFolder = true,
                    Children = selectedFiles.Where(f => f.Path.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase)).ToList()
                };

                radTreeViewFiles.ItemsSource = new List<FileItem> { rootItem };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nell'accesso alla cartella {folderPath}: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                var excludePaths = txtExcludePaths.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(p => p.Trim()).ToList();
                foreach (var exclude in excludePaths)
                {
                    if (filePath.IndexOf(exclude, StringComparison.OrdinalIgnoreCase) >= 0)
                        return false;
                }
            }

            string extension = System.IO.Path.GetExtension(filePath).ToLower();

            // Escludi estensioni specifiche
            if (!string.IsNullOrWhiteSpace(txtExcludeExtensions.Text))
            {
                var excludeExtensions = txtExcludeExtensions.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(ext => ext.StartsWith(".") ? ext.ToLower() : "." + ext.ToLower()).ToList();
                if (excludeExtensions.Contains(extension))
                    return false;
            }

            // Includi solo estensioni specifiche
            if (!string.IsNullOrWhiteSpace(txtIncludeExtensions.Text))
            {
                var includeExtensions = txtIncludeExtensions.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(ext => ext.StartsWith(".") ? ext.ToLower() : "." + ext.ToLower()).ToList();
                if (!includeExtensions.Contains(extension))
                    return false;
            }

            return true;
        }

        // Pulsante per rimuovere i file selezionati
        private void BtnRemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            var itemsToRemove = selectedFiles.Where(f => f.IsSelected).ToList();
            foreach (var item in itemsToRemove)
            {
                selectedFiles.Remove(item);
            }
            radTreeViewFiles.ItemsSource = selectedFiles;
        }

        // Pulsante per svuotare la lista
        private void BtnClearList_Click(object sender, RoutedEventArgs e)
        {
            selectedFiles.Clear();
            radTreeViewFiles.ItemsSource = selectedFiles;
        }

        

        // Pulsante per avviare il merging
        private async void BtnStartMerging_Click(object sender, RoutedEventArgs e)
        {
            var filesToMerge = new List<FileItem>();
            CollectSelectedFiles(radTreeViewFiles.Items, filesToMerge);

            if (filesToMerge.Count == 0)
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

            string outputFileName = txtOutputFileName.Text;
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
            // Raggruppa i file per estensione se necessario
            Dictionary<string, List<FileItem>> filesByExtension = new Dictionary<string, List<FileItem>>(StringComparer.OrdinalIgnoreCase);

            if (chkOneFilePerExtension.IsChecked == true)
            {
                foreach (var file in filesToMerge)
                {
                    token.ThrowIfCancellationRequested();
                    string extension = System.IO.Path.GetExtension(file.Path).TrimStart('.').ToLower();
                    if (!filesByExtension.ContainsKey(extension))
                    {
                        filesByExtension[extension] = new List<FileItem>();
                    }
                    filesByExtension[extension].Add(file);
                }
            }
            else
            {
                filesByExtension["all"] = filesToMerge;
            }

            int processedFiles = 0;

            foreach (var kvp in filesByExtension)
            {
                token.ThrowIfCancellationRequested();
                string extension = kvp.Key;
                string currentOutputFileName = outputFileName.Replace("<extension>", extension);
                string currentOutputFilePath = System.IO.Path.Combine(outputFolder, currentOutputFileName);

                if (chkOneFilePerExtension.IsChecked == true)
                {
                    // Crea il nome file se non esiste già
                    if (!currentOutputFileName.Contains("<extension>"))
                    {
                        // Nothing to do, already replaced
                    }
                    else
                    {
                        currentOutputFileName = "merged_" + extension + "." + extension;
                        currentOutputFilePath = System.IO.Path.Combine(outputFolder, currentOutputFileName);
                    }
                }

                // Gestisci la sovrascrittura
                if (File.Exists(currentOutputFilePath) && chkOverwriteFiles.IsChecked != true)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Il file {currentOutputFilePath} esiste già e l'opzione per sovrascrivere non è selezionata.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    return;
                }

                using (var writer = new StreamWriter(currentOutputFilePath, chkOverwriteFiles.IsChecked == true ? false : true))
                {
                    foreach (var fileItem in kvp.Value)
                    {
                        token.ThrowIfCancellationRequested();
                        string content = File.ReadAllText(fileItem.Path);
                        string fileExtension = System.IO.Path.GetExtension(fileItem.Path).TrimStart('.').ToLower();

                        // Aggiungi commenti in base all'estensione
                        string commentStart = GetCommentStartTypeForLanguage(fileExtension);
                        string commentEnd = GetCommentEndTypeForLanguage(fileExtension);

                        writer.WriteLine($"{commentStart} Inizio di {System.IO.Path.GetFileName(fileItem.Path)} {commentEnd}");
                        writer.WriteLine(content);
                        writer.WriteLine($"{commentStart} Fine di {System.IO.Path.GetFileName(fileItem.Path)} {commentEnd}");
                        writer.WriteLine();

                        processedFiles++;
                        Dispatcher.Invoke(() =>
                        {
                            progressBar.Value = processedFiles;
                            lblProgress.Content = $"{processedFiles} / {progressBar.Maximum}";
                        });
                    }
                }
            }
        }

        // Metodo per ottenere i simboli di commento
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
                    return "//"; // Default
            }
        }

        private static string GetCommentEndTypeForLanguage(string extension)
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

        // Metodo per raccogliere i file selezionati dalla RadTreeView
        private static void CollectSelectedFiles(ItemCollection items, List<FileItem> filesToMerge)
        {
            foreach (var obj in items)
            {
                if (obj is RadTreeViewItem treeViewItem)
                {
                    if (treeViewItem.DataContext is FileItem item)
                    {
                        if (item.IsSelected && !item.IsFolder)
                        {
                            filesToMerge.Add(item);
                        }
                        if (treeViewItem.Items != null && treeViewItem.Items.Count > 0)
                        {
                            CollectSelectedFiles(treeViewItem.Items, filesToMerge);
                        }
                    }
                }
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
                radTreeViewFiles.IsEnabled = isEnabled;
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
                IncludeSubfolders = chkIncludeSubfolders.IsChecked == true,
                ExcludeHidden = chkExcludeHidden.IsChecked == true,
                IncludeExtensions = txtIncludeExtensions.Text,
                ExcludeExtensions = txtExcludeExtensions.Text,
                ExcludePaths = txtExcludePaths.Text,
                OutputFolder = txtOutputFolder.Text,
                OutputFileName = txtOutputFileName.Text,
                OneFilePerExtension = chkOneFilePerExtension.IsChecked == true,
                OverwriteFiles = chkOverwriteFiles.IsChecked == true
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

                    chkIncludeSubfolders.IsChecked = config.IncludeSubfolders;
                    chkExcludeHidden.IsChecked = config.ExcludeHidden;
                    txtIncludeExtensions.Text = config.IncludeExtensions;
                    txtExcludeExtensions.Text = config.ExcludeExtensions;
                    txtExcludePaths.Text = config.ExcludePaths;
                    txtOutputFolder.Text = config.OutputFolder;
                    txtOutputFileName.Text = config.OutputFileName;
                    chkOneFilePerExtension.IsChecked = config.OneFilePerExtension;
                    chkOverwriteFiles.IsChecked = config.OverwriteFiles;

                    MessageBox.Show("Configurazione caricata con successo.", "Successo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Errore durante il caricamento della configurazione: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    // Classe per rappresentare un file o una cartella
    public class FileItem
    {
        public bool IsSelected { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public bool IsFolder { get; set; }
        public List<FileItem> Children { get; set; }
    }

    // Classe per la configurazione di merging
    public class MergingConfig
    {
        public bool IncludeSubfolders { get; set; }
        public bool ExcludeHidden { get; set; }
        public string IncludeExtensions { get; set; }
        public string ExcludeExtensions { get; set; }
        public string ExcludePaths { get; set; }
        public string OutputFolder { get; set; }
        public string OutputFileName { get; set; }
        public bool OneFilePerExtension { get; set; }
        public bool OverwriteFiles { get; set; }
    }
}
