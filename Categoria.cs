using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SCPP_WinUI_CS
{
    public class Categoria
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; }
    }
}
