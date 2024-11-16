// TreeViewExplorerViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using TreeViewFileExplorer.Events;
using TreeViewFileExplorer.Model;
using TreeViewFileExplorer.Services;

namespace TreeViewFileExplorer.ViewModels;

/// <summary>
/// ViewModel for the TreeView explorer.
/// </summary>
public class TreeViewExplorerViewModel : INotifyPropertyChanged
{
    private readonly IIconService _iconService;
    private readonly IFileSystemService _fileSystemService;
    private readonly IEventAggregator _eventAggregator;

    public ObservableCollection<IFileSystemObjectViewModel> RootItems { get; set; }
    public ObservableCollection<FileItem> SelectedFiles { get; }

    // Proprietà per mostrare/nascondere file nascosti
    private bool _showHiddenFiles;
    public bool ShowHiddenFiles
    {
        get => _showHiddenFiles;
        set
        {
            if (_showHiddenFiles != value)
            {
                _showHiddenFiles = value;
                OnPropertyChanged(nameof(ShowHiddenFiles));
                RefreshRootItems();
            }
        }
    }

    // Proprietà per il filtro regex
    private string _filterRegex;
    public string FilterRegex
    {
        get => _filterRegex;
        set
        {
            if (_filterRegex != value)
            {
                _filterRegex = value;
                OnPropertyChanged(nameof(FilterRegex));
                ApplyFilter();
            }
        }
    }

    // Proprietà per la navigazione a una cartella specifica
    private string _navigatePath;
    public string NavigatePath
    {
        get => _navigatePath;
        set
        {
            if (_navigatePath != value)
            {
                _navigatePath = value;
                OnPropertyChanged(nameof(NavigatePath));
            }
        }
    }

    // Proprietà per lo stato di caricamento
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }
    }

    // Comandi
    public ICommand ToggleHiddenFilesCommand { get; }
    public ICommand ApplyFilterCommand { get; }
    public ICommand NavigateToFolderCommand { get; }

    // Regex Pattern
    internal Regex _regexFilter;

    public TreeViewExplorerViewModel(IIconService iconService, IFileSystemService fileSystemService, IEventAggregator eventAggregator)
    {
        _iconService = iconService;
        _fileSystemService = fileSystemService;
        _eventAggregator = eventAggregator;

        RootItems = new ObservableCollection<IFileSystemObjectViewModel>();
        SelectedFiles = new ObservableCollection<FileItem>();

        InitializeRootItems();

        ToggleHiddenFilesCommand = new RelayCommand(ToggleHiddenFiles);
        ApplyFilterCommand = new RelayCommand(ApplyFilterCommandExecute);
        NavigateToFolderCommand = new RelayCommand(NavigateToFolder);

        _regexFilter = null;
    }

    private void ToggleHiddenFiles(object parameter)
    {
        ShowHiddenFiles = !ShowHiddenFiles;
    }

    private void ApplyFilterCommandExecute(object parameter)
    {
        // Apri una finestra di dialogo per inserire la regex
        var inputDialog = new Views.InputDialog("Inserisci la regex per filtrare file e cartelle:", "Filtro Regex");
        if (inputDialog.ShowDialog() == true)
        {
            FilterRegex = inputDialog.ResponseText;
        }
    }

    private void NavigateToFolder(object parameter)
    {
        // Apri una finestra di dialogo per selezionare la cartella
        var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog
        {
            IsFolderPicker = true,
            Title = "Seleziona una cartella"
        };

        if (dialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
        {
            NavigatePath = dialog.FileName;
            NavigateToPath(NavigatePath);
        }
    }

    private async void NavigateToPath(string path)
    {
        IsLoading = true;
        RootItems?.Clear();
        RootItems ??= new ObservableCollection<IFileSystemObjectViewModel>();

        try
        {
            var directoryInfo = new DirectoryInfo(path);
            if (directoryInfo.Exists)
            {
                var dirViewModel = new DirectoryViewModel(directoryInfo, _iconService, _fileSystemService, null, _eventAggregator, ShowHiddenFiles, _regexFilter);
                dirViewModel.PropertyChanged += OnFileSystemObjectPropertyChanged;
                RootItems.Add(dirViewModel);
            }
            else
            {
                MessageBox.Show($"La cartella {path} non esiste.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore nel navigare alla cartella: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(FilterRegex))
        {
            _regexFilter = null;
        }
        else
        {
            try
            {
                _regexFilter = new Regex(FilterRegex, RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Regex non valida: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                _regexFilter = null;
            }
        }
        RefreshRootItems();
    }

    private void RefreshRootItems()
    {
        IsLoading = true;
        RootItems.Clear();
        InitializeRootItems();
        IsLoading = false;
    }

    private void InitializeRootItems()
    {
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            var dirViewModel = new DirectoryViewModel(drive.RootDirectory, _iconService, _fileSystemService, null, _eventAggregator, ShowHiddenFiles, _regexFilter);
            dirViewModel.PropertyChanged += OnFileSystemObjectPropertyChanged;
            RootItems.Add(dirViewModel);
        }
    }

    private void OnFileSystemObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IFileSystemObjectViewModel.IsSelected))
        {
            var item = sender as IFileSystemObjectViewModel;
            if (item != null)
            {
                if (item.IsSelected)
                {
                    AddToSelectedFiles(item);
                    _eventAggregator.Publish(new BeforeExploreEvent(item.Path));
                }
                else
                {
                    RemoveFromSelectedFiles(item);
                }
            }
        }
    }

    private void AddToSelectedFiles(IFileSystemObjectViewModel item)
    {
        if (item is FileViewModel file)
        {
            SelectedFiles.Add(new FileItem
            {
                Name = file.Name,
                Path = file.Path,
                IsFolder = false
            });
        }
        else if (item is DirectoryViewModel dir)
        {
            SelectedFiles.Add(new FileItem
            {
                Name = dir.Name,
                Path = dir.Path,
                IsFolder = true
            });
        }
        OnPropertyChanged(nameof(SelectedFiles));
    }

    private void RemoveFromSelectedFiles(IFileSystemObjectViewModel item)
    {
        var existingItem = SelectedFiles.FirstOrDefault(f => f.Path.Equals(item.Path, StringComparison.OrdinalIgnoreCase));
        if (existingItem != null)
        {
            SelectedFiles.Remove(existingItem);
            OnPropertyChanged(nameof(SelectedFiles));
        }
    }

    private void OnBeforeExplore(BeforeExploreEvent e)
    {
        // Gestisci l'evento prima dell'esplorazione
        IsLoading = true;
    }

    private void OnAfterExplore(AfterExploreEvent e)
    {
        // Gestisci l'evento dopo l'esplorazione
        IsLoading = false;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}