using System.Windows;

namespace TreeViewFileExplorer.Views
{
    /// <summary>
    /// Interaction logic for InputDialog.xaml
    /// </summary>
    public partial class InputDialog : Window
    {
        public string ResponseText { get; private set; }

        public InputDialog(string question, string title = "Input")
        {
            InitializeComponent();
            this.Title = title;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = InputTextBox.Text;
            this.DialogResult = true;
        }
    }
}