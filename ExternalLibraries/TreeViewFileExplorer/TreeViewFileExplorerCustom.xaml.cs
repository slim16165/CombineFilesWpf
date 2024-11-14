using System;
using System.Windows;
using System.Windows.Controls;
using TreeViewFileExplorer.Events;
using TreeViewFileExplorer.Managers;
using TreeViewFileExplorer.Services;
using TreeViewFileExplorer.ViewModels;

namespace TreeViewFileExplorer
{
    /// <summary>
    /// Interaction logic for TreeViewFileExplorerCustom.xaml
    /// </summary>
    public partial class TreeViewFileExplorerCustom : UserControl
    {
        private readonly IIconService _iconService;
        private readonly IFileSystemService _fileSystemService;
        private readonly TreeViewExplorerViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeViewFileExplorerCustom"/> class.
        /// </summary>
        /// <param name="iconService">Optional custom icon service.</param>
        /// <param name="fileSystemService">Optional custom file system service.</param>
        public TreeViewFileExplorerCustom(IIconService iconService = null, IFileSystemService fileSystemService = null)
        {
            InitializeComponent();

            // Iniettiamo le dipendenze o usiamo quelle di default
            var shellManager = new ShellManager();
            _iconService = iconService ?? new IconService(shellManager);
            _fileSystemService = fileSystemService ?? new FileSystemService();
            _viewModel = new TreeViewExplorerViewModel(_iconService, _fileSystemService, new EventAggregator());
            DataContext = _viewModel;
        }

        private void RadTreeView_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            string[] droppedPaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var path in droppedPaths)
            {
                if (!_fileSystemService.IsAccessibleAsync(path).Result)
                {
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
                    return;
                }
            }

            e.Effects = DragDropEffects.Move | DragDropEffects.Copy;
            e.Handled = true;
        }

        private void RadTreeView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] droppedPaths = (string[])e.Data.GetData(DataFormats.FileDrop);

                var treeView = sender as Telerik.Windows.Controls.RadTreeView;
                var targetItem = treeView.InputHitTest(e.GetPosition(treeView)) as Telerik.Windows.Controls.RadTreeViewItem;

                if (targetItem != null)
                {
                    var targetViewModel = targetItem.DataContext as IFileSystemObjectViewModel;
                    if (targetViewModel != null && targetViewModel is DirectoryViewModel targetDirectory)
                    {
                        foreach (var path in droppedPaths)
                        {
                            string fileName = System.IO.Path.GetFileName(path);
                            string destPath = System.IO.Path.Combine(targetDirectory.Path, fileName);

                            try
                            {
                                if (System.IO.Directory.Exists(path))
                                {
                                    System.IO.Directory.Move(path, destPath);
                                }
                                else if (System.IO.File.Exists(path))
                                {
                                    System.IO.File.Move(path, destPath);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Errore nel cancellare {fileName}: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }

                        // Refresh la directory target
                        targetDirectory.Explore();
                    }
                }
            }
        }
    }
}
