using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SCPP_WinUI_CS.Models
{
    public partial class Asset : ObservableObject
    {
        // Esta clase se definio asi porque usando el [ObservableProperty] no se parseaba
        // el JSON, por no tener constructores publicos o algo asi. Se hizo de esta manera, utilizando
        // igual MVVM toolkit un poco mas manual pero permite parsear el JSON
        private int _id;
        private DateOnly _fecha;
        private string _descripcion;
        private string _assetData;
        private int _fkCategoria;

        public Asset()
        {
            Reset();
        }

        public void Reset()
        {
            Id = 0;
            Fecha = DateOnly.FromDateTime(DateTime.Now);
            Descripcion = "";
            AssetData = "";
            FkCategoria = 0;
        }

        public void Clone(Asset other)
        {
            // Para que se ejecute la reactividad
            Id = other.Id;
            Fecha = other.Fecha;
            Descripcion = other.Descripcion;
            AssetData = other.AssetData;
            FkCategoria = other.FkCategoria;
        }

        [JsonPropertyName("id")]
        public int Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        [JsonPropertyName("fecha")]
        public DateOnly Fecha
        {
            get { return _fecha; }
            set { SetProperty(ref _fecha, value); }
        }

        [JsonPropertyName("descripcion")]
        public string Descripcion
        {
            get { return _descripcion; }
            set { SetProperty(ref _descripcion, value); }
        }

        [JsonPropertyName("assetData")]
        public string AssetData
        {
            get { return _assetData; }
            set { SetProperty(ref _assetData, value); }
        }

        [JsonPropertyName("fk_categoria")]
        public int FkCategoria
        {
            get { return _fkCategoria; }
            set { SetProperty(ref _fkCategoria, value); }
        }
        // TODO add Categoria
    }
}
