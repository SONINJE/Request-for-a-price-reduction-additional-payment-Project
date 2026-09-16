using System.Collections.Generic;
using System.Windows;

namespace WpfPriceApp.Views
{
    public partial class AddProductDialog : Window
    {
        public string ItemName => ItemBox.Text.Trim();
        public string Channel => ChannelBox.Text.Trim();

        public AddProductDialog(IEnumerable<string> items, IEnumerable<string> channels)
        {
            InitializeComponent();
            ItemBox.ItemsSource = items;
            ChannelBox.ItemsSource = channels;
            Loaded += (_, _) => ItemBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ItemBox.Text))
            {
                MessageBox.Show("품목(상품명)을 입력하세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
