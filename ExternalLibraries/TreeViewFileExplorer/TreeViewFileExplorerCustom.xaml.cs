// TreeViewFileExplorerCustom.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using TreeViewFileExplorer.Events;
using TreeViewFileExplorer.Manager;
using TreeViewFileExplorer.Services;
using TreeViewFileExplorer.ViewModels;

namespace TreeViewFileExplorer;

/// <summary>
/// Interaction logic for TreeViewFileExplorerCustom.xaml
/// </summary>
public partial class TreeViewFileExplorerCustom : UserControl
{
    private readonly IIconService _iconService;
    private readonly IFileSystemService _fileSystemService;
    private readonly TreeViewExplorerViewModel _viewModel;

    public TreeViewFileExplorerCustom() : this(null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TreeViewFileExplorerCustom"/> class.
    /// </summary>
    /// <param name="iconService">Optional custom icon service.</param>
    /// <param name="fileSystemService">Optional custom file system service.</param>
    public TreeViewFileExplorerCustom(IIconService iconService = null, IFileSystemService fileSystemService = null)
    {
        InitializeComponent();

        // Iniettiamo le dipendenze o usiamo quelle di default
        var eventAggregator = new EventAggregator();
        var shellManager = new ShellManager();
        _iconService = iconService ?? new IconService(shellManager);
        _fileSystemService = fileSystemService ?? new FileSystemService(); // Inizializzazione corretta
        _viewModel = new TreeViewExplorerViewModel(_iconService, _fileSystemService, eventAggregator);
        DataContext = _viewModel;
    }

    // NOTE: I metodi RadTreeView_DragOver e RadTreeView_Drop sono stati rimossi perché:
    // 1. RadTreeView è commentato nel XAML e non è più utilizzato (riferimenti Telerik non disponibili in SDK-style)
    // 2. RadTreeView_DragOver usava .Result su async, causando potenziali deadlock
    // 3. Se in futuro si ripristina RadTreeView o si implementa drag & drop con controlli standard WPF,
    //    questi metodi dovranno essere riscritti senza usare .Result e con gestione async corretta
}