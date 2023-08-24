using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization.NumberFormatting;
using LiveChartsCore.SkiaSharpView;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using SCPP_WinUI_CS.Models;
using System.Collections.ObjectModel;

namespace SCPP_WinUI_CS
{
    // Esto es algo que cree yo, Noes un ViewModel como tal porque no estmaos usando Microsoft MVVM
    // Este archivo guarda la definicion de todas las cosas que necesite "El controlador" DashBoard.xaml.cs
    // Todas las variables existen aqui separando el contenido en 2 Archivos

    // En realidad esta Clase es la misma clase solo que tiene una definicion partial para que el
    // compilador la considere a la hora de traer los datos
    public partial class Dashboard : Page
    {
        public LabelVisual CatGraphTitle { get; set; } =
            new LabelVisual
            {
            Text = "Categorias 12 Meses",
            TextSize = 20,
            Paint = new SolidColorPaint(SKColors.Gray)
            };
        public LabelVisual HistoricTitle { get; set; } =
            new LabelVisual
            {
                Text = "Historico por Tipo Doc",
                TextSize = 20,
                Paint = new SolidColorPaint(SKColors.Gray)
            };

        // Para formatear dentro del NumberBox Repo de Ejemplo https://github.com/sonnemaf/NumberBoxTest/blob/master/NumberBoxTest/MainPage.xaml.cs
        private DecimalFormatter IntFormatter { get; } =
            new DecimalFormatter
            {
                IsGrouped = true,
                FractionDigits = 0,
                IntegerDigits = 1
            };

        public IEnumerable<ICartesianAxis> HistoricYAxis = new List<Axis>
            {
                new Axis
                {
                    // Now the Y axis we will display labels as currency
                    // LiveCharts provides some common formatters
                    // in this case we are using the currency formatter.
                    Labeler = Labelers.Currency,
                    LabelsPaint = new SolidColorPaint
                    {
                        Color =  SKColors.Gray
                    },

                    // you could also build your own currency formatter
                    // for example:
                    // Labeler = (value) => value.ToString("C")

                    // But the one that LiveCharts provides creates shorter labels when
                    // the amount is in millions or trillions
                }
            };

        public Documento newDoc = new();
        public Documento editDoc = new();
        public ScalarTransition MyOpacityTransition = new ScalarTransition()
        {
            Duration = TimeSpan.FromSeconds(0.5)
        };

        public GetDocsForm getDocsForm = new GetDocsForm
        {
            fk_tipoDoc = 1,
            searchPhrase = "",
            fk_categoria = 0,
            fechaInicio = new DateOnly(),
            fechaTermino = new DateOnly()
        };

        public ObservableCollection<Categoria> categorias { get; set; } = new();
        public ObservableCollection<TipoDoc> tipoDocs { get; set; } = new();

        public ObservableCollection<GridRow> gridRows { get; set; } = new();

        public struct GridRow
        {
            public string Proposito { get; set; }
            public string Monto { get; set; }
            public string Fecha { get; set; }
            public int Id { get; set; }
        }

        public ObservableString SumaTotalDocs = new();
    }
}
