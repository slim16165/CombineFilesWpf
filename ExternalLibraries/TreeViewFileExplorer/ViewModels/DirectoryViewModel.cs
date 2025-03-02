using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Services;
using TreeViewFileExplorer.Enums;
using TreeViewFileExplorer.Events;
using TreeViewFileExplorer.Services;

namespace TreeViewFileExplorer.ViewModels;

/// <summary>
/// ViewModel for directories.
/// </summary>
public class DirectoryViewModel : BaseFileSystemObjectViewModel
{
    private ImageSource _imageSource;
    private readonly IEventAggregator _eventAggregator;
    private readonly bool _showHiddenFiles;
    private readonly Regex _filterRegex;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryViewModel"/> class.
    /// </summary>
    /// <param name="directoryInfo">Informazioni sulla directory (System.IO.DirectoryInfo).</param>
    /// <param name="iconService">Servizio per ottenere le icone.</param>
    /// <param name="fileSystemService">Servizio per l'accesso asincrono al file system.</param>
    /// <param name="fileOperationsService">Servizio che implementa le operazioni di copia/move/rename/delete.</param>
    /// <param name="eventAggregator">Event aggregator per comunicare Before/AfterExplore.</param>
    /// <param name="showHiddenFiles">Indica se mostrare i file/cartelle nascosti.</param>
    /// <param name="filterRegex">Regex di filtro (può essere null se non usato).</param>
    public DirectoryViewModel(
        DirectoryInfo directoryInfo,
        IIconService iconService,
        IFileSystemService fileSystemService,
        IFileOperationsService fileOperationsService,
        IEventAggregator eventAggregator,
        bool showHiddenFiles,
        Regex filterRegex)
        : base(iconService, fileSystemService, fileOperationsService, showHiddenFiles, filterRegex)
    {
        _eventAggregator = eventAggregator;
        _showHiddenFiles = showHiddenFiles;
        _filterRegex = filterRegex;

        // Impostiamo Nome e Path dal DirectoryInfo
        Name = directoryInfo.Name;
        Path = directoryInfo.FullName;

        // Icona "chiusa" di default
        _imageSource = iconService.GetIcon(Path, ItemType.Folder, IconSize.Small, ItemState.Close);

        // Aggiunge un "dummy" per permettere il caricamento lazy (espansione).
        Children.Add(new DummyViewModel());
    }

    public override string Name { get; protected set; }
    public override string Path { get; protected set; }

    public override ImageSource ImageSource
    {
        get => _imageSource;
        protected set
        {
            _imageSource = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Esegue l'esplorazione della directory, caricando la lista di sottocartelle e file.
    /// </summary>
    public override async Task ExploreAsync()
    {
        // Se c'è solo il DummyViewModel, significa che dobbiamo effettivamente caricare
        if (Children.Count == 1 && Children[0] is DummyViewModel)
        {
            // Pubblica l'evento "prima di esplorare"
            _eventAggregator.Publish(new BeforeExploreEvent(Path));

            Children.Clear();

            try
            {
                // Carichiamo le sub-directory
                var directories = await FileSystemService.GetDirectoriesAsync(Path, _showHiddenFiles, _filterRegex);
                foreach (var dir in directories)
                {
                    var dirViewModel = new DirectoryViewModel(
                        dir,
                        IconService,
                        FileSystemService,
                        FileOperationsService,
                        _eventAggregator,
                        _showHiddenFiles,
                        _filterRegex);

                    dirViewModel.PropertyChanged += OnChildPropertyChanged;
                    Children.Add(dirViewModel);
                }

                // Carichiamo i file
                var files = await FileSystemService.GetFilesAsync(Path, _showHiddenFiles, _filterRegex);
                foreach (var file in files)
                {
                    var fileViewModel = new FileViewModel(
                        file,
                        IconService,
                        FileSystemService,
                        _showHiddenFiles,
                        _filterRegex);

                    fileViewModel.PropertyChanged += OnChildPropertyChanged;
                    Children.Add(fileViewModel);
                }

                // Impostiamo l'icona "cartella aperta"
                ImageSource = IconService.GetIcon(Path, ItemType.Folder, IconSize.Small, ItemState.Open);
            }
            catch (Exception ex)
            {
                // Gestione eccezioni di caricamento (log, msg all'utente, ecc.)
                // Qui puoi usare un logger, o sollevare un evento. 
                // Per semplicità lasciamo vuoto.
            }
            finally
            {
                // Pubblica l'evento "dopo l'esplorazione"
                _eventAggregator.Publish(new AfterExploreEvent(Path));
            }
        }
    }

    /// <summary>
    /// Reagisce ai cambiamenti di proprietà dei figli (ad es. IsSelected).
    /// </summary>
    private void OnChildPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Se cambia IsSelected su un figlio, potremmo voler riflettere sul genitore
        // (dipende dalla tua logica).
        if (e.PropertyName == nameof(IsSelected))
        {
            // Esempio: aggiorniamo la notifica
            OnPropertyChanged(nameof(IsSelected));
        }
    }
}