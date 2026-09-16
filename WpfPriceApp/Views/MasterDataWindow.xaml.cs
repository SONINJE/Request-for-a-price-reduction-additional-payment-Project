using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace WpfPriceApp.Views
{
    /// <summary>
    /// 채널/품목 마스터 목록을 추가/삭제하는 창.
    /// 호출자가 넘겨준 ObservableCollection 을 그대로 편집하므로, 이 창을 닫으면
    /// 곧바로 최신 목록이 반영되어 있다 (호출자가 그 뒤에 파일로 저장하면 됨).
    /// </summary>
    public partial class MasterDataWindow : Window
    {
        private readonly ObservableCollection<string> _channels;
        private readonly ObservableCollection<string> _items;

        public MasterDataWindow(ObservableCollection<string> channels, ObservableCollection<string> items)
        {
            InitializeComponent();
            _channels = channels;
            _items = items;
            ChannelList.ItemsSource = _channels;
            ItemList.ItemsSource = _items;
        }

        private void AddChannel_Click(object sender, RoutedEventArgs e)
        {
            AddUnique(_channels, NewChannelBox);
        }

        private void RemoveChannel_Click(object sender, RoutedEventArgs e)
        {
            if (ChannelList.SelectedItem is string s) _channels.Remove(s);
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            AddUnique(_items, NewItemBox);
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (ItemList.SelectedItem is string s) _items.Remove(s);
        }

        private static void AddUnique(ObservableCollection<string> list, System.Windows.Controls.TextBox box)
        {
            string value = box.Text.Trim();
            if (string.IsNullOrEmpty(value)) return;
            if (!list.Any(x => x == value)) list.Add(value);
            box.Text = "";
        }

        private void Close_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}
