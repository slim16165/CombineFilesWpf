using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TreeViewFileExplorer.Model;
using TreeViewFileExplorer.Services;

namespace TreeViewFileExplorer.ViewModels
{
    /// <summary>
    /// Base class for file system object ViewModels.
    /// </summary>
    public abstract class BaseFileSystemObjectViewModel : IFileSystemObjectViewModel, INotifyPropertyChanged
    {
        protected readonly IIconService IconService;
        protected readonly IFileSystemService FileSystemService;

        public BaseFileSystemObjectViewModel(IIconService iconService, IFileSystemService fileSystemService)
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
                        Explore();
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

        protected virtual void Copy(object parameter)
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

        private string PromptForDestinationPath()
        {
            // Implementa un dialogo per selezionare la destinazione
            // Per semplicità, restituisce una stringa fissa o usa un dialogo di sistema
            return "C:\\DestinationPath"; // Placeholder
        }


        public abstract void Explore();

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
                // Optionally remove from parent collection
            }
            catch (Exception ex)
            {
                // Log or handle exception
            }
        }

        protected virtual void Rename(object parameter)
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
                    MessageBox.Show(string.Format("Errore nel renaming", Name, ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Nome non valido.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }



        private string PromptForNewName()
        {
            // Implement a dialog to get the new name from the user
            // For simplicity, returning a placeholder
            return "NewName";
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
    }
}
