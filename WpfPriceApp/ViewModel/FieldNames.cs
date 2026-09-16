using System.Collections.Generic;

namespace WpfPriceApp.ViewModel
{
    /// <summary>
    /// PriceCalcEngine(C++) 의 Field 상수와 반드시 동일한 키를 사용해야 한다.
    /// 순서는 UI 표시 순서(엑셀 원본 컬럼 순서)와 동일하게 유지.
    /// </summary>
    public static class FieldNames
    {
        public const string SalePrice = "salePrice";
        public const string SupplyPrice = "supplyPrice";
        public const string ShippingCost = "shippingCost";
        public const string FeeRate = "feeRate";
        public const string ExistingSubsidy = "existingSubsidy";
        public const string Cost = "cost";
        public const string RealCost = "realCost";
        public const string MarginAmount = "marginAmount";
        public const string RequestPlus = "requestPlus";
        public const string RequestMinus = "requestMinus";
        public const string FinalMargin = "finalMargin";
        public const string FinalMarginRate = "finalMarginRate";
        public const string ExpectedQty = "expectedQty";
        public const string ExpectedCost = "expectedCost";

        // 표시 순서 + 한글 라벨
        public static readonly (string Key, string Label)[] All = new[]
        {
            (SalePrice,       "판매가"),
            (SupplyPrice,     "공급가"),
            (ShippingCost,    "배송비"),
            (FeeRate,         "수수료"),
            (ExistingSubsidy, "기존지원금"),
            (Cost,            "원가"),
            (RealCost,        "실원가"),
            (MarginAmount,    "마진액"),
            (RequestPlus,     "추가요청금액(+)"),
            (RequestMinus,    "추가요청금액(-)"),
            (FinalMargin,     "최종마진액"),
            (FinalMarginRate, "최종마진율"),
            (ExpectedQty,     "예상수량"),
            (ExpectedCost,    "예상 추가 비용(-)"),
        };

        // 신규 상품 추가 시 기본으로 "고정(입력)" 표시할 7개 필드
        public static readonly string[] DefaultLocked = new[]
        {
            SalePrice, ShippingCost, FeeRate, ExistingSubsidy, Cost, RequestPlus, ExpectedQty
        };

        public static readonly int RequiredKnownCount = 7; // 변수14 - 공식7
    }
}
