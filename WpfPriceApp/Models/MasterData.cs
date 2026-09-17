using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WpfPriceApp.Models
{
    /// <summary>
    /// 품목(SKU) 마스터 한 줄 — 실제 엑셀의 "Master" 시트에 대응.
    /// 상품 추가 시 상품명으로 이 목록을 찾아 SKU/매입가/기존정산액/채널을 자동으로 채운다
    /// (원본 엑셀의 VLOOKUP과 같은 역할). 값은 그 뒤에도 자유롭게 덮어쓸 수 있다.
    /// </summary>
    public class MasterProduct
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = ""; // 설명(상품명) — 조회 키

        [JsonPropertyName("sku")]
        public string Sku { get; set; } = "";

        [JsonPropertyName("purchasePrice")]
        public double PurchasePrice { get; set; } // 매입가 (VAT+)

        [JsonPropertyName("eventPrice")]
        public double EventPrice { get; set; } // 행사가

        [JsonPropertyName("spotPrice")]
        public double SpotPrice { get; set; } // 스팟가

        [JsonPropertyName("subsidySpot")]
        public double SubsidySpot { get; set; } // 지원금(vat+)_S — 기존정산액(+) 기본값으로 쓰임

        [JsonPropertyName("subsidyEvent")]
        public double SubsidyEvent { get; set; } // 지원금(vat+)_E

        [JsonPropertyName("subsidyRegular")]
        public double SubsidyRegular { get; set; } // 지원금(vat+)_R

        [JsonPropertyName("channel")]
        public string Channel { get; set; } = ""; // 주력 채널 (예: 11번가, SSF, 토스)

        [JsonPropertyName("note")]
        public string Note { get; set; } = "";
    }

    /// <summary>MasterData.json 전체 내용 — 품목 마스터 목록.</summary>
    public class MasterData
    {
        [JsonPropertyName("products")]
        public List<MasterProduct> Products { get; set; } = new();
    }
}
