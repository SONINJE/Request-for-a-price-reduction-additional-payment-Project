using System;
using System.Collections.Generic;
using System.Globalization;

namespace WpfPriceApp.ViewModel
{
    /// <summary>
    /// 상품 그리드의 컬럼 순서를 한 곳에서 정의한다. 그리드 컬럼 순서(XAML), CSV 내보내기 순서,
    /// 엑셀에서 복사한 값을 붙여넣을 때의 열 매핑이 전부 이 순서 하나를 따른다.
    ///
    /// 실제로 쓰고 있는 최신 엑셀의 컬럼 순서(D~X, 행사명/행사시작/행사종료는 행사 단위라 제외)를
    /// 그대로 따른다 — 다만 "채널"은 실제 엑셀에서 품목 마스터에 상품별로 미리 지정되는 값이라
    /// 그 자체로 별도 컬럼을 두었다(원본은 "채널&gt;공급가" 처럼 공급가 헤더에 묶여 있음).
    /// </summary>
    public static class GridColumns
    {
        public class Spec
        {
            public string Header { get; }
            public Func<ProductRowViewModel, string> GetCsvText { get; }
            public Action<ProductRowViewModel, string> SetFromPastedText { get; }

            public Spec(string header, Func<ProductRowViewModel, string> getCsvText,
                        Action<ProductRowViewModel, string> setFromPastedText)
            {
                Header = header;
                GetCsvText = getCsvText;
                SetFromPastedText = setFromPastedText;
            }
        }

        /// <summary>"1,234" / "12.3%" / "0.123" 등 그리드나 엑셀에서 흔히 보이는 형식을 숫자로 해석한다.
        /// %가 붙어 있으면 100으로 나눠 비율로 맞춘다(수수료/최종마진율처럼 0~1 비율로 저장되는 값용).</summary>
        private static double ParseNumber(string text)
        {
            text = text.Trim();
            bool isPercent = text.Contains('%');
            text = text.Replace("%", "").Replace(",", "").Trim();
            if (!double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double v)) return 0;
            return isPercent ? v / 100.0 : v;
        }

        private static string NumText(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);

        public static readonly IReadOnlyList<Spec> All = new List<Spec>
        {
            new("SKU",                 vm => vm.Sku,     (vm, t) => vm.Sku = t.Trim()),
            new("상품명",               vm => vm.Name,     (vm, t) => vm.Name = t.Trim()),
            new("채널",                 vm => vm.Channel,  (vm, t) => vm.Channel = t.Trim()),
            new("행사유형",             vm => vm.EventType,(vm, t) => vm.EventType = t.Trim()),
            new("판매가",               vm => NumText(vm.SalePrice),             (vm, t) => vm.SalePrice = ParseNumber(t)),
            new("채널>공급가",          vm => NumText(vm.SupplyPrice),           (vm, t) => vm.SupplyPrice = ParseNumber(t)),
            new("배송비",               vm => NumText(vm.ShippingCost),          (vm, t) => vm.ShippingCost = ParseNumber(t)),
            new("수수료",               vm => NumText(vm.FeeRate),               (vm, t) => vm.FeeRate = ParseNumber(t)),
            new("기존정산액(+)",        vm => NumText(vm.ExistingSettlement),    (vm, t) => vm.ExistingSettlement = ParseNumber(t)),
            new("매입가(+)",            vm => NumText(vm.PurchasePrice),         (vm, t) => vm.PurchasePrice = ParseNumber(t)),
            new("정산액포함매입가(+)",  vm => NumText(vm.SettledPurchasePrice),  (vm, t) => vm.SettledPurchasePrice = ParseNumber(t)),
            new("캡슐금액",             vm => NumText(vm.CapsuleAmount),         (vm, t) => vm.CapsuleAmount = ParseNumber(t)),
            new("쿠폰액 분담 쿠폰",     vm => NumText(vm.CouponAmount),          (vm, t) => vm.CouponAmount = ParseNumber(t)),
            new("마진액",               vm => NumText(vm.MarginAmount),          (vm, t) => vm.MarginAmount = ParseNumber(t)),
            new("추가요청금액(+)",      vm => NumText(vm.RequestPlus),           (vm, t) => vm.RequestPlus = ParseNumber(t)),
            new("추가요청금액(-)",      vm => NumText(vm.RequestMinus),          (vm, t) => vm.RequestMinus = ParseNumber(t)),
            new("최종마진액",           vm => NumText(vm.FinalMargin),           (vm, t) => vm.FinalMargin = ParseNumber(t)),
            new("최종마진율",           vm => NumText(vm.FinalMarginRate),       (vm, t) => vm.FinalMarginRate = ParseNumber(t)),
            new("본사(확인용)vat+",     vm => NumText(vm.HqCheckVatPlus),        (vm, t) => vm.HqCheckVatPlus = ParseNumber(t)),
            new("본사(확인용)vat-",     vm => NumText(vm.HqCheckVatMinus),       (vm, t) => vm.HqCheckVatMinus = ParseNumber(t)),
            new("기지원금",             vm => NumText(vm.SupportAmount),         (vm, t) => vm.SupportAmount = ParseNumber(t)),
            new("비고",                 vm => vm.Note,     (vm, t) => vm.Note = t.Trim()),
        };
    }
}
