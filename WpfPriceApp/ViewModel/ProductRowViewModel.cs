using System.ComponentModel;
using WpfPriceApp.Models;

namespace WpfPriceApp.ViewModel
{
    /// <summary>
    /// DataGrid 표시용 래퍼. 값 편집/잠금 토글은 하단 상세 편집 패널(FieldEditorViewModel)에서도
    /// 할 수 있고, 그리드 셀을 엑셀처럼 직접 입력/붙여넣기 해서 바꿀 수도 있다 (아래 setter들).
    /// 그리드에서 값을 직접 바꿔도 잠금(고정) 상태 자체는 건드리지 않는다 — 엑셀에서 셀 값을
    /// 바로 쓰는 것과 같은 감각으로, 역산 대상 여부와는 별개로 동작한다.
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

        public string Sku
        {
            get => Model.Sku;
            set { Model.Sku = value; OnPropertyChanged(nameof(Sku)); }
        }

        public string Channel
        {
            get => Model.Channel;
            set { Model.Channel = value; OnPropertyChanged(nameof(Channel)); }
        }

        public string EventType
        {
            get => Model.EventType;
            set { Model.EventType = value; OnPropertyChanged(nameof(EventType)); }
        }

        public string Note
        {
            get => Model.Note;
            set { Model.Note = value; OnPropertyChanged(nameof(Note)); }
        }

        private double GetV(string key) => Model.GetValue(key);
        private void SetV(string key, double value, string propName)
        {
            Model.Values[key] = value;
            OnPropertyChanged(propName);
        }

        // 그리드 컬럼 바인딩용 — 직접 입력/붙여넣기로 값을 바꿀 수 있다.
        public double SalePrice { get => GetV(FieldNames.SalePrice); set => SetV(FieldNames.SalePrice, value, nameof(SalePrice)); }
        public double SupplyPrice { get => GetV(FieldNames.SupplyPrice); set => SetV(FieldNames.SupplyPrice, value, nameof(SupplyPrice)); }
        public double ShippingCost { get => GetV(FieldNames.ShippingCost); set => SetV(FieldNames.ShippingCost, value, nameof(ShippingCost)); }
        public double FeeRate { get => GetV(FieldNames.FeeRate); set => SetV(FieldNames.FeeRate, value, nameof(FeeRate)); }
        public double ExistingSettlement { get => GetV(FieldNames.ExistingSettlement); set => SetV(FieldNames.ExistingSettlement, value, nameof(ExistingSettlement)); }
        public double PurchasePrice { get => GetV(FieldNames.PurchasePrice); set => SetV(FieldNames.PurchasePrice, value, nameof(PurchasePrice)); }
        public double SettledPurchasePrice { get => GetV(FieldNames.SettledPurchasePrice); set => SetV(FieldNames.SettledPurchasePrice, value, nameof(SettledPurchasePrice)); }
        public double CapsuleAmount { get => GetV(FieldNames.CapsuleAmount); set => SetV(FieldNames.CapsuleAmount, value, nameof(CapsuleAmount)); }
        public double CouponAmount { get => GetV(FieldNames.CouponAmount); set => SetV(FieldNames.CouponAmount, value, nameof(CouponAmount)); }
        public double MarginAmount { get => GetV(FieldNames.MarginAmount); set => SetV(FieldNames.MarginAmount, value, nameof(MarginAmount)); }
        public double RequestPlus { get => GetV(FieldNames.RequestPlus); set => SetV(FieldNames.RequestPlus, value, nameof(RequestPlus)); }
        public double RequestMinus { get => GetV(FieldNames.RequestMinus); set => SetV(FieldNames.RequestMinus, value, nameof(RequestMinus)); }
        public double FinalMargin { get => GetV(FieldNames.FinalMargin); set => SetV(FieldNames.FinalMargin, value, nameof(FinalMargin)); }
        public double FinalMarginRate { get => GetV(FieldNames.FinalMarginRate); set => SetV(FieldNames.FinalMarginRate, value, nameof(FinalMarginRate)); }
        public double HqCheckVatPlus { get => GetV(FieldNames.HqCheckVatPlus); set => SetV(FieldNames.HqCheckVatPlus, value, nameof(HqCheckVatPlus)); }
        public double HqCheckVatMinus { get => GetV(FieldNames.HqCheckVatMinus); set => SetV(FieldNames.HqCheckVatMinus, value, nameof(HqCheckVatMinus)); }
        public double SupportAmount { get => GetV(FieldNames.SupportAmount); set => SetV(FieldNames.SupportAmount, value, nameof(SupportAmount)); }

        /// <summary>계산 결과가 반영된 후 그리드 컬럼들을 다시 그리도록 통지.</summary>
        public void RefreshAll() => OnPropertyChanged(string.Empty);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
