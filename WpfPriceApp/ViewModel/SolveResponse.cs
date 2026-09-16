using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WpfPriceApp.ViewModel
{
    /// <summary>PriceCalcEngine.dll 의 PC_SolveRow 응답 JSON 매핑.</summary>
    public class SolveResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("values")]
        public Dictionary<string, double> Values { get; set; } = new();

        [JsonPropertyName("unresolved")]
        public List<string> Unresolved { get; set; } = new();

        [JsonPropertyName("conflicts")]
        public List<string> Conflicts { get; set; } = new();
    }
}
