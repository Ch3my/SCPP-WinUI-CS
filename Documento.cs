using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SCPP_WinUI_CS
{
    public class Documento
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("proposito")] 
        public string Proposito { get; set; }
        [JsonPropertyName("monto")]
        public int Monto { get; set; }
        [JsonPropertyName("fk_categoria")]
        public int FkCategoria { get; set; }
        [JsonPropertyName("fk_tipoDoc")]
        public int FkTipoDoc{ get; set; }
        [JsonPropertyName("fecha")]
        public DateOnly Fecha { get; set; }
        [JsonPropertyName("categoria")]
        public Categoria Categoria{ get; set; }
    }
}
