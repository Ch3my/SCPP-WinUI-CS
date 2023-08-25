using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore;
using Microsoft.UI.Xaml;
using SCPP_WinUI_CS.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveChartsCore.SkiaSharpView;
using Windows.Globalization.NumberFormatting;
using static SCPP_WinUI_CS.Dashboard;

namespace SCPP_WinUI_CS.PageModels
{
    public partial class DashboardPageViewModel : ObservableObject
    {
        public DashboardPageViewModel()
        {
            this._searchPhrase = string.Empty;
            this._fk_tipoDoc = 1;
        }

        [ObservableProperty]
        private Documento _newDoc = new();

        [ObservableProperty]
        private Documento _editDoc = new();

        public ObservableCollection<GridRow> GridRows { get; set; } = new();
        public ObservableCollection<Categoria> Categorias { get; set; } = new();
        public ObservableCollection<TipoDoc> TipoDocs { get; set; } = new();

        [ObservableProperty]
        private string _searchPhrase;
        [ObservableProperty]
        private int _fk_tipoDoc;
        [ObservableProperty]
        private int _fk_categoria;

        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaTermino { get; set; }

        [ObservableProperty]
        private string _sumaTotalDocs;

        // NOTA creo que esto no esta funcionando
        public ScalarTransition MyOpacityTransition = new ScalarTransition()
        {
            Duration = TimeSpan.FromSeconds(0.5)
        };
        public struct GridRow
        {
            public string Proposito { get; set; }
            public string Monto { get; set; }
            public string Fecha { get; set; }
            public int Id { get; set; }
        }
    }
}
