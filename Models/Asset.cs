using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SCPP_WinUI_CS.Models
{
    public class Asset
    {
        public Asset()
        {
        }
        public void Reset()
        {
            this.Id = 0;
            this.Fecha = default;
            this.Descripcion = "";
            this.AssetData = "";
        }

        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("fecha")]
        public DateOnly Fecha { get; set; }
        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; }
        [JsonPropertyName("assetData")]
        public string AssetData { get; set; }
        // TODO add Categoria
    }
}
