using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SCPP_WinUI_CS.Models
{
    public struct DataItem
    {
        [JsonPropertyName("label")]
        public string Label { get; set; }
        [JsonPropertyName("data")]
        public int Data { get; set; }
        [JsonPropertyName("catId")]
        public int CatId { get; set; }
    }

    public struct BarGraphApiStruct
    {
        [JsonPropertyName("labels")]

        public string[] Labels { get; set; }
        [JsonPropertyName("amounts")]
        public double[] Amounts { get; set; }
        [JsonPropertyName("data")]
        public List<DataItem> Data { get; set; }

    }
}
