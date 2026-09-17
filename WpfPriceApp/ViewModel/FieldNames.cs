using System.Collections.Generic;

namespace WpfPriceApp.ViewModel
{
    /// <summary>
    /// PriceCalcEngine(C++) 의 Field 상수와 반드시 동일한 키를 사용해야 한다.
    /// 순서는 UI 표시 순서(실제 엑셀 컬럼 순서 G~W)와 동일하게 유지.
    /// 2026-09 개편: 실제로 쓰고 있는 최신 엑셀(품목 마스터 VLOOKUP 기반) 구조로 갱신.
    /// </summary>
    public static class FieldNames
    {
        public const string SalePrice = "salePrice";
        public const string SupplyPrice = "supplyPrice";
        public const string ShippingCost = "shippingCost";
        public const string FeeRate = "feeRate";
        public const string ExistingSettlement = "existingSettlement";
        public const string PurchasePrice = "purchasePrice";
        public const string SettledPurchasePrice = "settledPurchasePrice";
        public const string CapsuleAmount = "capsuleAmount";
        public const string CouponAmount = "couponAmount";
        public const string MarginAmount = "marginAmount";
        public const string RequestPlus = "requestPlus";
        public const string RequestMinus = "requestMinus";
        public const string FinalMargin = "finalMargin";
        public const string FinalMarginRate = "finalMarginRate";
        public const string HqCheckVatPlus = "hqCheckVatPlus";
        public const string HqCheckVatMinus = "hqCheckVatMinus";
        public const string SupportAmount = "supportAmount";

        // 표시 순서 + 한글 라벨. 잠금(고정)/역산 대상이 되는 16개 필드만 포함한다.
        // 기지원금(SupportAmount)은 ROUNDUP 때문에 역으로 풀 수 없는 단방향 계산값이라
        // 이 목록(그리고 값 편집 잠금 패널)에는 넣지 않는다 — 표에는 별도로 표시한다.
        public static readonly (string Key, string Label)[] All = new[]
        {
            (SalePrice,             "판매가"),
            (SupplyPrice,           "채널>공급가"),
            (ShippingCost,          "배송비"),
            (FeeRate,               "수수료"),
            (ExistingSettlement,    "기존정산액(+)"),
            (PurchasePrice,         "매입가(+)"),
            (SettledPurchasePrice,  "정산액포함매입가(+)"),
            (CapsuleAmount,         "캡슐금액"),
            (CouponAmount,          "쿠폰액 분담 쿠폰"),
            (MarginAmount,          "마진액"),
            (RequestPlus,           "추가요청금액(+)"),
            (RequestMinus,          "추가요청금액(-)"),
            (FinalMargin,           "최종마진액"),
            (FinalMarginRate,       "최종마진율"),
            (HqCheckVatPlus,        "본사(확인용)vat+"),
            (HqCheckVatMinus,       "본사(확인용)vat-"),
        };

        // 신규 상품 추가 시 기본으로 "고정(입력)" 표시할 필드 (자유도=8과 같은 개수)
        public static readonly string[] DefaultLocked = new[]
        {
            SalePrice, ShippingCost, FeeRate, ExistingSettlement, PurchasePrice, RequestPlus,
            CapsuleAmount, CouponAmount
        };

        public static readonly int RequiredKnownCount = 8; // 변수16 - 공식8
    }
}
