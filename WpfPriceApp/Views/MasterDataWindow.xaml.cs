using System.Collections.ObjectModel;
using System.Windows;
using WpfPriceApp.Models;

namespace WpfPriceApp.Views
{
    /// <summary>
    /// 품목(SKU) 마스터를 추가/수정/삭제하는 창. 호출자가 넘겨준 ObservableCollection 을
    /// 그대로 편집하므로, 이 창을 닫으면 곧바로 최신 목록이 반영되어 있다
    /// (호출자가 그 뒤에 파일로 저장하면 됨).
    /// </summary>
    public partial class MasterDataWindow : Window
    {
        private readonly ObservableCollection<MasterProduct> _products;

        public MasterDataWindow(ObservableCollection<MasterProduct> products)
        {
            InitializeComponent();
            _products = products;
            ProductGrid.ItemsSource = _products;
        }

        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            var p = new MasterProduct { Name = "새 품목" };
            _products.Add(p);
            ProductGrid.SelectedItem = p;
            ProductGrid.ScrollIntoView(p);
        }

        private void RemoveRow_Click(object sender, RoutedEventArgs e)
        {
            if (ProductGrid.SelectedItem is MasterProduct p) _products.Remove(p);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            ProductGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true);
            DialogResult = true;
        }
    }
}
