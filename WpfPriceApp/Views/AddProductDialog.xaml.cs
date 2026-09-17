using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using WpfPriceApp.Models;

namespace WpfPriceApp.Views
{
    /// <summary>
    /// 상품 추가 창 — 품목 마스터 목록을 그대로 보여주고, 이 창에서 바로 새 품목을 등록하거나
    /// 필요 없는 품목을 지울 수 있다(호출자가 넘긴 ObservableCollection 을 직접 편집하므로
    /// 창을 닫으면 곧바로 반영됨).
    ///
    /// 검색창(SearchBox)은 목록 필터링 전용이고, 새 품목 등록은 별도 카드의 NewNameBox/등록
    /// 버튼으로 완전히 분리했다 — 예전에는 검색창이 "찾기"와 "새 이름 입력"을 겸해서 헷갈렸다.
    ///
    /// SKU/채널은 선택한 품목 마스터 항목에 바로 저장되지만, 행사유형/비고는 품목 마스터가
    /// 아니라 "지금 이 행사에 추가하는 상품 행" 에만 적용되는 값이라 선택 여부와 관계없이
    /// 항상 입력할 수 있고, Ok_Click 에서 EventType/Note 프로퍼티로 꺼내 쓴다.
    /// </summary>
    public partial class AddProductDialog : Window
    {
        private readonly ObservableCollection<MasterProduct> _masterProducts;
        private readonly ICollectionView _view;
        private bool _isLoadingSelection;

        public string ItemName { get; private set; } = "";
        public string EventType { get; private set; } = "";
        public string Note { get; private set; } = "";

        public AddProductDialog(ObservableCollection<MasterProduct> masterProducts)
        {
            InitializeComponent();
            _masterProducts = masterProducts;
            _view = CollectionViewSource.GetDefaultView(_masterProducts);
            _view.SortDescriptions.Add(new SortDescription(nameof(MasterProduct.Name), ListSortDirection.Ascending));
            ProductList.ItemsSource = _view;
            Loaded += (_, _) => SearchBox.Focus();
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string filter = SearchBox.Text.Trim();
            _view.Filter = filter.Length == 0
                ? null
                : (obj => obj is MasterProduct p && p.Name.Contains(filter, System.StringComparison.OrdinalIgnoreCase));
        }

        private void ProductList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _isLoadingSelection = true;
            if (ProductList.SelectedItem is MasterProduct p)
            {
                SelectionHint.Text = $"'{p.Name}' 선택됨 — 아래에서 SKU/채널을 입력하거나 수정하세요.";
                EditPanel.Visibility = Visibility.Visible;
                SkuBox.Text = p.Sku;
                ChannelBox.Text = p.Channel;
            }
            else
            {
                SelectionHint.Text = " ";
                EditPanel.Visibility = Visibility.Collapsed;
            }
            _isLoadingSelection = false;
        }

        private void EditField_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isLoadingSelection) return;
            if (ProductList.SelectedItem is not MasterProduct p) return;

            p.Sku = SkuBox.Text.Trim();
            p.Channel = ChannelBox.Text.Trim();

            // MasterProduct 는 INotifyPropertyChanged 를 구현하지 않으므로, 목록에 보이는
            // "SKU ... · 채널 ... · 매입가 ..." 요약 텍스트를 갱신하려면 뷰를 강제로 새로고침해야 한다.
            _view.Refresh();
        }

        private void AddMasterProduct_Click(object sender, RoutedEventArgs e)
        {
            string name = NewNameBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("등록할 상품명을 입력하세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            foreach (var existing in _masterProducts)
            {
                if (existing.Name == name)
                {
                    ProductList.SelectedItem = existing;
                    ProductList.ScrollIntoView(existing);
                    return;
                }
            }
            var p = new MasterProduct { Name = name };
            _masterProducts.Add(p);
            NewNameBox.Text = "";
            ProductList.SelectedItem = p;
            ProductList.ScrollIntoView(p);
            SkuBox.Focus();
        }

        private void RemoveMasterProduct_Click(object sender, RoutedEventArgs e)
        {
            if (ProductList.SelectedItem is not MasterProduct p) return;
            var confirm = MessageBox.Show($"품목 마스터에서 '{p.Name}'을(를) 삭제할까요?", "품목 삭제",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
            _masterProducts.Remove(p);
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            ItemName = ProductList.SelectedItem is MasterProduct p ? p.Name : NewNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(ItemName))
            {
                MessageBox.Show("품목(상품명)을 목록에서 고르거나 \"새 품목 등록\"에 입력하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            EventType = EventTypeBox.Text.Trim();
            Note = NoteBox.Text.Trim();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
