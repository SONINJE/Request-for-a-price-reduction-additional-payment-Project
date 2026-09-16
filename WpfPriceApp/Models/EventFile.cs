using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WpfPriceApp.Models
{
    /// <summary>
    /// 행사 하나 = JSON 파일 하나 (C++ 쪽 EventFile 과 동일 스키마).
    /// </summary>
    public class EventFile
    {
        [JsonPropertyName("eventName")]
        public string EventName { get; set; } = "새 행사";

        [JsonPropertyName("startDate")]
        public string StartDate { get; set; } = "";

        [JsonPropertyName("endDate")]
        public string EndDate { get; set; } = "";

        [JsonPropertyName("memo")]
        public string Memo { get; set; } = "";

        [JsonPropertyName("products")]
        public List<ProductRow> Products { get; set; } = new();
    }
}
