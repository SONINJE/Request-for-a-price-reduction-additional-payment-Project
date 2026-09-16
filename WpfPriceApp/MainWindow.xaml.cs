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
            if (sender is ListBoxItem item && item.Content is string path &&
                DataContext is MainViewModel vm)
            {
                vm.LoadEvent(path);
            }
        }
    }
}
