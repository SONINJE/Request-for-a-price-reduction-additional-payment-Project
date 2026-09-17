using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WpfPriceApp.Models
{
    /// <summary>
    /// 상품 한 행. C++ 엔진(EventModel.h 의 ProductRow)과 동일한 JSON 스키마.
    /// </summary>
    public class ProductRow
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("sku")]
        public string Sku { get; set; } = "";

        [JsonPropertyName("channel")]
        public string Channel { get; set; } = "";

        [JsonPropertyName("eventType")]
        public string EventType { get; set; } = "";

        [JsonPropertyName("note")]
        public string Note { get; set; } = "";

        [JsonPropertyName("values")]
        public Dictionary<string, double> Values { get; set; } = new();

        [JsonPropertyName("lockedFields")]
        public List<string> LockedFields { get; set; } = new();

        public double GetValue(string key) => Values.TryGetValue(key, out var v) ? v : 0.0;
        public bool IsLocked(string key) => LockedFields.Contains(key);
    }
}
