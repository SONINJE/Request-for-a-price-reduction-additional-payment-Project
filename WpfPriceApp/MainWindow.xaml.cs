using System.Windows;
using System.Windows.Controls;
using WpfPriceApp.ViewModel;

namespace WpfPriceApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void EventFile_DoubleClick(object sender, RoutedEventArgs e)
        {
            if (sender is ListBoxItem item && item.Content is EventListItem entry &&
                DataContext is MainViewModel vm)
            {
                vm.LoadEvent(entry.FilePath);
            }
        }
    }
}
