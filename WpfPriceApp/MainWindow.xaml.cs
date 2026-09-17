using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        // 그리드의 "현재 셀"이 바뀔 때마다(=아무 셀이나 클릭/키보드 이동) 그 행을 값 편집 패널에 띄운다.
        // DataGrid.SelectedItem 을 뷰모델에 직접 바인딩하지 않는 이유: SelectionUnit=CellOrRowHeader 로
        // 열 단위 다중 선택(세로 한 줄 복사)을 지원하려면 셀 선택을 써야 하는데, WPF는 SelectedItem 을
        // 코드에서 바꿀 때(행사 로드 시 첫 상품 자동 선택 등) 이 조합에서 예외를 던지는 제약이 있다.
        // CurrentCell 은 이 제약이 없어 안전하고, 편집 패널 갱신 목적은 그대로 달성된다.
        private void ProductsGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            if (ProductsGrid.CurrentCell.Item is ProductRowViewModel row) vm.SelectedRow = row;
        }

        // 상품 표에서 Ctrl+V 로 엑셀처럼 여러 행/열을 한 번에 붙여넣는다.
        // 복사(Ctrl+C)는 DataGrid 기본 기능(SelectionUnit=CellOrRowHeader, ClipboardCopyMode=ExcludeHeader)으로
        // 이미 동작한다 — 열 하나만 세로로 선택해서 복사하는 것도 이 설정 덕분에 가능하다.
        private void ProductsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.V || Keyboard.Modifiers != ModifierKeys.Control) return;
            if (DataContext is not MainViewModel vm) return;
            e.Handled = true;
            PasteIntoGrid(vm);
        }

        private void PasteIntoGrid(MainViewModel vm)
        {
            if (!Clipboard.ContainsText()) return;

            var currentCell = ProductsGrid.CurrentCell;
            if (currentCell.Column == null || currentCell.Item is not ProductRowViewModel anchorRow) return;

            int startRowIndex = vm.Rows.IndexOf(anchorRow);
            int startColIndex = currentCell.Column.DisplayIndex;
            if (startRowIndex < 0) return;

            string clipboardText = Clipboard.GetText().Replace("\r\n", "\n").TrimEnd('\n');
            var pastedRows = clipboardText.Split('\n');

            int rowsWritten = 0;
            bool rowsTruncated = false, colsTruncated = false;
            var touchedRows = new System.Collections.Generic.HashSet<ProductRowViewModel>();

            for (int r = 0; r < pastedRows.Length; r++)
            {
                int targetRowIndex = startRowIndex + r;
                if (targetRowIndex >= vm.Rows.Count) { rowsTruncated = true; break; }

                var targetRow = vm.Rows[targetRowIndex];
                var cells = pastedRows[r].Split('\t');
                for (int c = 0; c < cells.Length; c++)
                {
                    int targetColIndex = startColIndex + c;
                    if (targetColIndex >= GridColumns.All.Count) { colsTruncated = true; break; }
                    GridColumns.All[targetColIndex].SetFromPastedText(targetRow, cells[c]);
                }
                touchedRows.Add(targetRow);
                rowsWritten++;
            }

            if (rowsWritten == 0) return;

            // 지금 상세 편집 패널에 보이는 상품이 붙여넣기 대상이었으면 값 카드도 새로 고친다.
            if (vm.SelectedRow != null && touchedRows.Contains(vm.SelectedRow))
                vm.SelectedRow = vm.SelectedRow;

            string msg = $"{rowsWritten}행에 붙여넣었습니다.";
            if (rowsTruncated) msg += " (행 수가 모자라 일부는 붙여넣지 못했습니다 — 상품을 먼저 추가하세요.)";
            if (colsTruncated) msg += " (열이 넘쳐서 일부는 무시했습니다.)";
            vm.StatusMessage = msg;
        }
    }
}
