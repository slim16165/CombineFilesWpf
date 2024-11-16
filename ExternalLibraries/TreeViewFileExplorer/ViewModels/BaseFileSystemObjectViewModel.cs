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
using CombineFiles.Core;
using CombineFiles.Core.Helpers;
// Per FileSystemHelper.IsValidName, se lo vuoi mantenere
using TreeViewFileExplorer.Model;
using TreeViewFileExplorer.Services;
using TreeViewFileExplorer.Views;

namespace TreeViewFileExplorer.ViewModels;

/// <summary>
/// Base class for file system object ViewModels.
/// </summary>
public abstract class BaseFileSystemObjectViewModel : IFileSystemObjectViewModel, INotifyPropertyChanged
{
    protected readonly IIconService IconService;
    protected readonly IFileSystemService FileSystemService;
    protected readonly IFileOperationsService FileOperationsService;

    /// <summary>
    /// Costruttore base per i ViewModel di file system (file e cartelle).
    /// </summary>
    protected BaseFileSystemObjectViewModel(
        IIconService iconService,
        IFileSystemService fileSystemService,
        IFileOperationsService fileOperationsService,
        bool showHiddenFiles,
        Regex filterRegex)
    {
        IconService = iconService;
        FileSystemService = fileSystemService;
        FileOperationsService = fileOperationsService;

        // Collezione di figli (file o directory)
        Children = new ObservableCollection<IFileSystemObjectViewModel>();

        // Comandi base
        OpenCommand = new RelayCommand(Open);
        DeleteCommand = new RelayCommand(Delete);
        RenameCommand = new RelayCommand(Rename);
        CopyCommand = new RelayCommand(Copy);
        MoveCommand = new RelayCommand(Move);
    }

    // Proprietà astratte da implementare in DirectoryViewModel e FileViewModel
    public abstract string Name { get; protected set; }
    public abstract string Path { get; protected set; }
    public abstract ImageSource ImageSource { get; protected set; }

    // Lista di figli (per le cartelle sarà la lista di file e sotto-cartelle)
    public ObservableCollection<IFileSystemObjectViewModel> Children { get; }

    // Gestione selezione/espansione (tipico di treeview in WPF)
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

                // All'espansione, diamo il via all'esplorazione (lazy load)
                if (_isExpanded)
                {
                    // Chiamata asincrona (ignorata) per caricare i figli
                    ExploreAsync();
                }
            }
        }
    }

    // Comandi
    public ICommand OpenCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RenameCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand MoveCommand { get; }

    // Metodi di business logici (copia, move, rename, delete) che usano IFileOperationsService
    protected void Copy(object parameter)
    {
        string destinationPath = PromptForDestinationPath();
        if (!string.IsNullOrWhiteSpace(destinationPath))
        {
            try
            {
                bool isDir = this is DirectoryViewModel;
                FileOperationsService.Copy(Path, System.IO.Path.Combine(destinationPath, Name), isDir);

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
        string destinationPath = PromptForDestinationPath();
        if (!string.IsNullOrWhiteSpace(destinationPath))
        {
            try
            {
                bool isDir = this is DirectoryViewModel;
                FileOperationsService.Move(Path, System.IO.Path.Combine(destinationPath, Name), isDir);

                MessageBox.Show($"Spostato {Name} in {destinationPath}", "Successo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nello spostare {Name}: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    protected void Rename(object parameter)
    {
        // Chiediamo all'utente il nuovo nome
        string newName = PromptForNewName();
        if (!string.IsNullOrWhiteSpace(newName) && FileSystemHelper.IsValidName(newName))
        {
            try
            {
                bool isDir = this is DirectoryViewModel;
                FileOperationsService.Rename(Path, newName, isDir);

                // Aggiorna le proprietà del ViewModel
                Name = newName;
                string parentDir = System.IO.Path.GetDirectoryName(Path);
                Path = System.IO.Path.Combine(parentDir, newName);

                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(Path));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel rinominare {Name}: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            MessageBox.Show("Nome non valido.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    protected virtual void Delete(object parameter)
    {
        try
        {
            bool isDir = this is DirectoryViewModel;
            FileOperationsService.Delete(Path, isDir);

            // Se siamo riusciti a cancellare, rimuoviamo il riferimento dal genitore
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

    // Invocato quando l'utente vuole "aprire" un file o una cartella (e.g. con doppio click)
    protected virtual void Open(object parameter)
    {
        try
        {
            // Per file apriamo con l'app predefinita, per cartelle apriamo Explorer, etc.
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            // Qui se vuoi puoi avvisare l'utente o loggare
        }
    }

    /// <summary>
    /// Prompt che mostra all'utente un dialog per scegliere la cartella di destinazione (ad es. per Copy/Move).
    /// </summary>
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

    /// <summary>
    /// Chiede all'utente il nuovo nome (usato da Rename).
    /// </summary>
    protected string PromptForNewName()
    {
        var inputDialog = new InputDialog("Inserisci il nuovo nome:", "Rinomina");
        inputDialog.Owner = Application.Current.MainWindow; // Facoltativo, se vuoi modal su MainWindow

        if (inputDialog.ShowDialog() == true)
        {
            return inputDialog.ResponseText;
        }

        return string.Empty;
    }

    /// <summary>
    /// Se devi trovare il ViewModel genitore per rimuovere la tua istanza da Children, implementalo qui.
    /// O passa un riferimento di Parent nel costruttore, a seconda della tua struttura.
    /// </summary>
    private IFileSystemObjectViewModel FindParentViewModel()
    {
        // Logica a piacere: potresti salvare un reference a "Parent" nel costruttore,
        // oppure percorrere l'albero in qualche modo. Lascio un placeholder:
        return null;
    }

    /// <summary>
    /// Metodo astratto che (DirectoryViewModel) implementa per caricare file/ cartelle.
    /// </summary>
    public abstract Task ExploreAsync();

    // INotifyPropertyChanged per il binding
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// Esempio di metodo helper per "filtrare" la visibilità in base a regole di UI
    /// (mostrare/nascondere file nascosti, applicare regex, ecc.).
    /// </summary>
    protected bool IsVisible(string name, bool isDirectory)
    {
        var mainViewModel = Application.Current.MainWindow.DataContext as TreeViewExplorerViewModel;
        if (mainViewModel == null)
            return true;

        // Se non vogliamo mostrare i file/cartelle nascoste
        if (!mainViewModel.ShowHiddenFiles && name.StartsWith("."))
            return false;

        // Se esiste una regex di filtro
        if (mainViewModel.FilterRegex != null && mainViewModel._regexFilter != null)
        {
            return mainViewModel._regexFilter.IsMatch(name);
        }

        return true;
    }
}