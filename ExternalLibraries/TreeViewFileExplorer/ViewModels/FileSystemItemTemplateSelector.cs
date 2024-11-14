using System.Windows;
using System.Windows.Controls;

namespace TreeViewFileExplorer.ViewModels
{
    public class FileSystemItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FileTemplate { get; set; }
        public DataTemplate DirectoryTemplate { get; set; }
        public DataTemplate DummyTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is FileViewModel)
                return FileTemplate;
            if (item is DirectoryViewModel)
                return DirectoryTemplate;
            if (item is DummyViewModel)
                return DummyTemplate;
            return base.SelectTemplate(item, container);
        }
    }
}