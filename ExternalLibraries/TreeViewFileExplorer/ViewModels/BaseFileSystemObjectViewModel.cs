using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TreeViewFileExplorer.Model;
using TreeViewFileExplorer.Services;
using TreeViewFileExplorer.Views;

namespace TreeViewFileExplorer.ViewModels
{
    /// <summary>
    /// Base class for file system object ViewModels.
    /// </summary>
    public abstract class BaseFileSystemObjectViewModel : IFileSystemObjectViewModel, INotifyPropertyChanged
    {
        protected readonly IIconService IconService;
        protected readonly IFileSystemService FileSystemService;

        protected BaseFileSystemObjectViewModel(IIconService iconService, IFileSystemService fileSystemService, bool showHiddenFiles, Regex filterRegex)
        {
            IconService = iconService;
            FileSystemService = fileSystemService;
            Children = new ObservableCollection<IFileSystemObjectViewModel>();

            OpenCommand = new RelayCommand(Open);
            DeleteCommand = new RelayCommand(Delete);
            RenameCommand = new RelayCommand(Rename);
            CopyCommand = new RelayCommand(Copy);
            MoveCommand = new RelayCommand(Move);
        }

        public abstract string Name { get; protected set; }

        public abstract string Path { get; protected set; }

        public abstract ImageSource ImageSource { get; protected set; }

        public ObservableCollection<IFileSystemObjectViewModel> Children { get; }

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isExpanded;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                    if (_isExpanded)
                    {
                        ExploreAsync();
                    }
                }
            }
        }

        public ICommand OpenCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand MoveCommand { get; }

        protected void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Ottieni informazioni sulla directory sorgente
            var dir = new DirectoryInfo(sourceDir);

            // Controlla se la directory sorgente esiste
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
            }

            // Se la directory di destinazione non esiste, creala
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            // Copia i file nella directory di destinazione
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = System.IO.Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, overwrite: true);
            }

            // Se ricorsivo, copia le sottodirectory
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dir.GetDirectories())
                {
                    string newDestinationDir = System.IO.Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        protected void Copy(object parameter)
        {
            // Implementa la logica per copiare l'oggetto
            string destinationPath = PromptForDestinationPath();
            if (!string.IsNullOrWhiteSpace(destinationPath))
            {
                try
                {
                    if (this is DirectoryViewModel dir)
                    {
                        string targetPath = System.IO.Path.Combine(destinationPath, dir.Name);
                        CopyDirectory(dir.Path, targetPath, recursive: true);
                    }
                    else if (this is FileViewModel file)
                    {
                        string targetPath = System.IO.Path.Combine(destinationPath, file.Name);
                        System.IO.File.Copy(file.Path, targetPath, overwrite: true);
                    }
                    MessageBox.Show($"Copiato {Name} in {destinationPath}", "Successo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Errore nel copiare {Name}: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected virtual void Move(object parameter)
        {
            // Implementa la logica per spostare l'oggetto
            string destinationPath = PromptForDestinationPath();
            if (!string.IsNullOrWhiteSpace(destinationPath))
            {
                try
                {
                    if (this is DirectoryViewModel dir)
                    {
                        System.IO.Directory.Move(Path, destinationPath);
                    }
                    else if (this is FileViewModel file)
                    {
                        System.IO.File.Move(Path, destinationPath);
                    }
                    MessageBox.Show($"Moved {Name} to {destinationPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error moving {Name}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected string PromptForDestinationPath()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Seleziona la cartella di destinazione"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return dialog.FileName;
            }

            return string.Empty;
        }

        public abstract Task ExploreAsync();

        protected virtual void Open(object parameter)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // Log or handle exception
            }
        }

        protected virtual void Delete(object parameter)
        {
            try
            {
                if (this is DirectoryViewModel dir)
                {
                    System.IO.Directory.Delete(Path, true);
                }
                else if (this is FileViewModel file)
                {
                    System.IO.File.Delete(Path);
                }

                // Rimuovi dall'elenco dei figli del genitore
                var parent = FindParentViewModel();
                if (parent != null)
                {
                    parent.Children.Remove(this);
                }

                MessageBox.Show($"{Name} è stato eliminato con successo.", "Successo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nell'eliminare {Name}: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private IFileSystemObjectViewModel FindParentViewModel()
        {
            // Implementa la logica per trovare il genitore di questo ViewModel
            // Potrebbe essere necessario passare una riferimento al genitore nel costruttore
            return null;
        }


        protected void Rename(object parameter)
        {
            string newName = PromptForNewName();
            if (!string.IsNullOrWhiteSpace(newName) && IsValidName(newName))
            {
                try
                {
                    string newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path), newName);
                    if (this is DirectoryViewModel dir)
                    {
                        System.IO.Directory.Move(Path, newPath);
                    }
                    else if (this is FileViewModel file)
                    {
                        System.IO.File.Move(Path, newPath);
                    }
                    // Aggiorna le proprietà
                    Name = newName;
                    Path = newPath;
                    OnPropertyChanged(nameof(Name));
                    OnPropertyChanged(nameof(Path));
                }
                catch (Exception ex)
                {
                    // Correzione dell'errore nell'uso di string.Format
                    MessageBox.Show($"Errore nel rinominare {Name}: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Nome non valido.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        protected string PromptForNewName()
        {
            var inputDialog = new InputDialog("Inserisci il nuovo nome:", "Rinomina");
            inputDialog.Owner = Application.Current.MainWindow; // Imposta la finestra principale come proprietaria

            if (inputDialog.ShowDialog() == true)
            {
                return inputDialog.ResponseText;
            }

            return string.Empty;
        }

        private bool IsValidName(string name)
        {
            // Controlla se il nome contiene caratteri non validi
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            return !name.Any(c => invalidChars.Contains(c));
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool IsVisible(string name, bool isDirectory)
        {
            // Ottieni il ViewModel principale per accedere ai filtri
            var mainViewModel = Application.Current.MainWindow.DataContext as TreeViewExplorerViewModel;
            if (mainViewModel == null)
                return true;

            // Verifica la visibilità dei file nascosti
            if (!mainViewModel.ShowHiddenFiles && name.StartsWith("."))
                return false;

            // Verifica il filtro regex
            if (mainViewModel.FilterRegex != null && mainViewModel._regexFilter != null)
            {
                return mainViewModel._regexFilter.IsMatch(name);
            }

            return true;
        }
    }
}
