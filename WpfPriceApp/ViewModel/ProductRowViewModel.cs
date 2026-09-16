using System.ComponentModel;
using WpfPriceApp.Models;

namespace WpfPriceApp.ViewModel
{
    /// <summary>
    /// DataGrid 표시용 래퍼. 그리드는 "읽기 전용 요약 보기"이고,
    /// 실제 값 편집/잠금 토글은 하단 상세 편집 패널(FieldEditorViewModel)에서 이루어진다.
    /// (어떤 필드든 잠그고/풀 수 있으므로 역산이 특정 컬럼에 한정되지 않는다.)
    /// </summary>
    public class ProductRowViewModel : INotifyPropertyChanged
    {
        public ProductRow Model { get; }

        public ProductRowViewModel(ProductRow model) => Model = model;

        public string Name
        {
            get => Model.Name;
            set { Model.Name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string Channel
        {
            get => Model.Channel;
            set { Model.Channel = value; OnPropertyChanged(nameof(Channel)); }
        }

        public string Note
        {
            get => Model.Note;
            set { Model.Note = value; OnPropertyChanged(nameof(Note)); }
        }

        // 그리드 컬럼 바인딩용 편의 속성 (읽기 전용 표시)
        public double SalePrice => Model.GetValue(FieldNames.SalePrice);
        public double SupplyPrice => Model.GetValue(FieldNames.SupplyPrice);
        public double ShippingCost => Model.GetValue(FieldNames.ShippingCost);
        public double FeeRate => Model.GetValue(FieldNames.FeeRate);
        public double ExistingSubsidy => Model.GetValue(FieldNames.ExistingSubsidy);
        public double Cost => Model.GetValue(FieldNames.Cost);
        public double RealCost => Model.GetValue(FieldNames.RealCost);
        public double MarginAmount => Model.GetValue(FieldNames.MarginAmount);
        public double RequestPlus => Model.GetValue(FieldNames.RequestPlus);
        public double RequestMinus => Model.GetValue(FieldNames.RequestMinus);
        public double FinalMargin => Model.GetValue(FieldNames.FinalMargin);
        public double FinalMarginRate => Model.GetValue(FieldNames.FinalMarginRate);
        public double ExpectedQty => Model.GetValue(FieldNames.ExpectedQty);
        public double ExpectedCost => Model.GetValue(FieldNames.ExpectedCost);

        /// <summary>계산 결과가 반영된 후 그리드 컬럼들을 다시 그리도록 통지.</summary>
        public void RefreshAll() => OnPropertyChanged(string.Empty);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
