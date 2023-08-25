using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SCPP_WinUI_CS
{
    public class Documento : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private int _Id;
        private string _Proposito;
        private int _Monto;
        private int? _FkCategoria;
        private int _FkTipoDoc;
        private DateOnly _Fecha;

        public Documento()
        {

        }

        [JsonPropertyName("id")]
        public int Id
        {
            get { return _Id; }
            set
            {
                _Id = value;
                OnPropertyChanged();
            }
        }
        [JsonPropertyName("proposito")]
        public string Proposito
        {
            get
            {
                return _Proposito;
            }
            set
            {
                _Proposito = value;
                OnPropertyChanged();
            }
        }
        [JsonPropertyName("monto")]
        public int Monto
        {
            get
            {
                return _Monto;
            }
            set
            {
                _Monto = value;
                OnPropertyChanged();
            }
        }
        [JsonPropertyName("fk_categoria")]
        public int? FkCategoria
        {
            get
            {
                return _FkCategoria;
            }
            set
            {
                if (_FkCategoria == value) { return; }
                _FkCategoria = value;
                OnPropertyChanged();
            }
        }
        [JsonPropertyName("fk_tipoDoc")]
        public int FkTipoDoc
        {
            get
            {
                return _FkTipoDoc;
            }
            set
            {
                if (_FkTipoDoc == value)
                {
                    return;
                }
                _FkTipoDoc = value;
                OnPropertyChanged();
            }
        }
        [JsonPropertyName("fecha")]
        public DateOnly Fecha
        {
            get { return _Fecha; }
            set
            {
                _Fecha = value;
                OnPropertyChanged();
            }
        }
        [JsonPropertyName("categoria")]
        public Categoria Categoria { get; set; }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Reset()
        {
            this.Id = 0;
            this.Monto = 0;
            this.Proposito = "";
            this.Fecha = DateOnly.FromDateTime(DateTime.Now);
            this.FkCategoria = 0;
            this.FkTipoDoc = 1;
            this.Categoria = null;
        }
    }
}
