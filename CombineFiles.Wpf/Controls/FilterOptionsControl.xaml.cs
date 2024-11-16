using System.Windows.Controls;

namespace CombineFilesWpf.Controls;

public partial class FilterOptionsControl : UserControl
{
    public FilterOptionsControl()
    {
        InitializeComponent();
    }

    // Proprietà per accedere ai controlli dal MainWindow
    public bool IncludeSubfolders => chkIncludeSubfolders.IsChecked == true;
    public bool ExcludeHidden => chkExcludeHidden.IsChecked == true;
    public string IncludeExtensions => txtIncludeExtensions.Text;
    public string ExcludeExtensions => txtExcludeExtensions.Text;
    public string ExcludePaths => txtExcludePaths.Text;

    // Metodo per disabilitare/abilitare i controlli
    public void ToggleControls(bool isEnabled)
    {
        chkIncludeSubfolders.IsEnabled = isEnabled;
        chkExcludeHidden.IsEnabled = isEnabled;
        txtIncludeExtensions.IsEnabled = isEnabled;
        txtExcludeExtensions.IsEnabled = isEnabled;
        txtExcludePaths.IsEnabled = isEnabled;
    }
}