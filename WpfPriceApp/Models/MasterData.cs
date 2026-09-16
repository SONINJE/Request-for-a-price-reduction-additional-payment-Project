using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WpfPriceApp.Models
{
    /// <summary>
    /// 상품 추가 시 선택할 수 있는 채널/품목 목록. MasterData.json 으로 저장된다.
    /// </summary>
    public class MasterData
    {
        [JsonPropertyName("channels")]
        public List<string> Channels { get; set; } = new();

        [JsonPropertyName("items")]
        public List<string> Items { get; set; } = new();
    }
}
