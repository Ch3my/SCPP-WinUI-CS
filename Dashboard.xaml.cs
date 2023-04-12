// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SCPP_WinUI_CS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Xml.Linq;
using Windows.Globalization;
using Windows.Globalization.NumberFormatting;
using Windows.UI.Core;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using System.Timers;
using Windows.Media.Protection.PlayReady;
using System.Threading.Tasks;

using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Defaults;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace SCPP_WinUI_CS
{
    /// <summary>
    /// Pagina donde se pueden agregar Documentos, editar, consultar y ver algunos graficos
    /// </summary>
    public sealed partial class Dashboard : Page
    {
        // Para formatear dentro del NumberBox Repo de Ejemplo https://github.com/sonnemaf/NumberBoxTest/blob/master/NumberBoxTest/MainPage.xaml.cs
        private DecimalFormatter IntFormatter { get; } =
            new DecimalFormatter
            {
                IsGrouped = true,
                FractionDigits = 0,
                IntegerDigits = 1
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
        public struct GridRow
        {
            public string Proposito { get; set; }
            public string Monto { get; set; }
            public string Fecha { get; set; }
            public int Id { get; set; }

        }
        ObservableCollection<Categoria> categorias = new ObservableCollection<Categoria>();
        ObservableCollection<TipoDoc> tipoDocs = new ObservableCollection<TipoDoc>();

        public ObservableCollection<GridRow> gridRows { get; set; } = new();
        public IEnumerable<ICartesianAxis> HistoricYAxis = new List<Axis>
            {
                new Axis
                {
                    // Now the Y axis we will display labels as currency
                    // LiveCharts provides some common formatters
                    // in this case we are using the currency formatter.
                    Labeler = Labelers.Currency 

                    // you could also build your own currency formatter
                    // for example:
                    // Labeler = (value) => value.ToString("C")

                    // But the one that LiveCharts provides creates shorter labels when
                    // the amount is in millions or trillions
                }
            };

        public Dashboard()
        {
            this.InitializeComponent();
            // Al iniciar setea las fecha Inicio y termino antes de llamar a API
            this.SetFechaIniTerTipoDoc();
            this.GetCategorias();
            this.GetTipoDoc();
            this.GetDocs();
            this.BuildHistoricChart();
        }
        public void SetFechaIniTerTipoDoc()
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            getDocsForm.fechaInicio = new DateOnly(today.Year, today.Month, 1);

            DateTime startOfNextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1);
            DateTime lastDayOfMonth = startOfNextMonth.AddDays(-1);
            getDocsForm.fechaTermino = DateOnly.FromDateTime(lastDayOfMonth.Date);
        }
        async public void GetCategorias()
        {
            HttpResponseMessage response = await App.httpClient.GetAsync(
               $"/categorias?sessionHash={App.sessionHash}"
               );
            response.EnsureSuccessStatusCode();
            List<Categoria> categoriaList = JsonSerializer.Deserialize<List<Categoria>>(response.Content.ReadAsStringAsync().Result);
            // Creamos Categoria Todos
            Categoria allCat = new Categoria
            {
                Id = 0,
                Descripcion = "(Todos)"
            };
            categorias.Clear();
            categorias.Add(allCat);
            foreach (Categoria c in categoriaList)
            {
                categorias.Add(c);
            }
            getDocsFormCategInput.SelectedIndex = 0;
        }
        async public void GetTipoDoc()
        {
            HttpResponseMessage response = await App.httpClient.GetAsync(
               $"/tipo-docs?sessionHash={App.sessionHash}"
               );
            response.EnsureSuccessStatusCode();
            List<TipoDoc> tipoDocList = JsonSerializer.Deserialize<List<TipoDoc>>(response.Content.ReadAsStringAsync().Result);
            tipoDocs.Clear();
            foreach (TipoDoc t in tipoDocList)
            {
                tipoDocs.Add(t);
            }
            // Comenzamos con el primer elemento que correspode a Id = 1
            getDocsFormTipoDocInput.SelectedIndex = 0;
        }
        async public void GetDocs()
        {
            HelperFunctions helperfns = new HelperFunctions();
            string urlParams = helperfns.BuildUrlParamsFromClass(getDocsForm);

            HttpResponseMessage response = await App.httpClient.GetAsync(
               $"/documentos?sessionHash={App.sessionHash}&{urlParams}"
               );
            // response.EnsureSuccessStatusCode se podria cambiar a otro tipo de control de Error
            if (!response.IsSuccessStatusCode)
            {
                return;
            }
            // Toma el JSON y lo parsea a una lista de Documentos
            List<Documento> docsList = JsonSerializer.Deserialize<List<Documento>>(response.Content.ReadAsStringAsync().Result);
            gridRows.Clear();

            // Por ahora la unica manera que el UI se refresque
            // problemas de reactividad al asignar el ObservableCollection
            foreach (Documento d in docsList)
            {
                GridRow thisRow = new GridRow();
                thisRow.Proposito = d.Proposito;
                thisRow.Monto = d.Monto.ToString("#,##0.##");
                thisRow.Fecha = d.Fecha.ToString("dd-MM-yyyy");
                thisRow.Id = d.Id;
                gridRows.Add(thisRow);
            }
        }

        private void DataGrid_RowEditEnded(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridRowEditEndedEventArgs e)
        {
            // Evento se dispara despues de que se actualizaron los objetos
            if (e.EditAction == CommunityToolkit.WinUI.UI.Controls.DataGridEditAction.Commit)
            {
                // Get the edited row and perform any necessary actions
                GridRow editedItem = (GridRow)e.Row.DataContext;
                // TODO actualizar Registro en API
            }
        }

        private void DataGrid_RowEditEnding(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridRowEditEndingEventArgs e)
        {
            // Evento se dispara antes de que actualice los objetos WINUI
            // se puede utilizar para validar antes de guardar en el objeto de la tabla
        }

        private void AddDoc_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            AgregarDocumentoBlade.IsOpen = true;
            // Por defecto ya va la fecha de Hoy
            newDocFechaInput.Date = DateTimeOffset.Now;
        }

        async private void SaveNewDoc_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (double.IsNaN(montoInput.Value))
            {
                // TODO mostrar mensaje de Error
                return;
            }
            if (newDocFechaInput.Date == null)
            {
                // TODO mostrar mensaje de Error
                return;
            }

            JsonObject test = new JsonObject();
            test.Add("sessionHash", App.sessionHash);
            test.Add("fk_categoria", newDoc.FkCategoria);
            test.Add("fk_tipoDoc", newDoc.FkTipoDoc);
            test.Add("proposito", newDoc.Proposito);
            test.Add("monto", newDoc.Monto);
            test.Add("fecha", newDoc.Fecha.ToString("yyyy-MM-dd"));

            HttpResponseMessage response = await App.httpClient.PostAsJsonAsync(
               $"/documentos", test, new JsonSerializerOptions
               {
                   DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
               });

            if (!response.IsSuccessStatusCode)
            {
                DocumentosNotification.Content = "Durante la comunicacion con la API";
                DocumentosNotification.Show(3000);
            }
            this.GetDocs();
            newDoc.Reset();

            DocumentosNotification.Content = "Documento Grabado correctamente";
            DocumentosNotification.Show(3000);
        }

        private void getDocsFormFecIniInput_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            //getDocsForm.fechaInicio = 
        }

        private void RefreshDocs_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            this.GetDocs();
        }

        async private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                editDoc.Reset();
                return;
            }
            GridRow selectedRow = (GridRow)e.AddedItems[0];

            Dictionary<string, object> urlArg = new Dictionary<string, object>();
            urlArg["sessionHash"] = App.sessionHash;
            urlArg["id[]"] = selectedRow.Id;

            HelperFunctions helperfns = new HelperFunctions();
            string urlParams = helperfns.BuildUrlParamsFromDictionary(urlArg);

            HttpResponseMessage response = await App.httpClient.GetAsync(
               $"/documentos?{urlParams}"
               );
            if (!response.IsSuccessStatusCode)
            {
                // Err Show Error MSG
                return;
            }
            List<Documento> docsList = JsonSerializer.Deserialize<List<Documento>>(response.Content.ReadAsStringAsync().Result);
            editDoc.Reset();
            editDoc.Monto = docsList[0].Monto;
            editDoc.Id = docsList[0].Id;
            editDoc.Proposito = docsList[0].Proposito;
            editDoc.FkTipoDoc = docsList[0].FkTipoDoc;
            editDoc.FkCategoria = docsList[0].FkCategoria;
            editDoc.Fecha = docsList[0].Fecha;
            EditDocumentoBlade.IsOpen = true;
        }

        async private void SaveEditDoc_Click(object sender, RoutedEventArgs e)
        {
            // Se deja la implementacion de Timer solo como ejemplo de como modificar el UI
            // desde otro hilo
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            JsonObject apiArg = new JsonObject();
            apiArg.Add("sessionHash", App.sessionHash);
            apiArg.Add("id", editDoc.Id);
            apiArg.Add("fk_categoria", editDoc.FkCategoria);
            apiArg.Add("fk_tipoDoc", editDoc.FkTipoDoc);
            apiArg.Add("proposito", editDoc.Proposito);
            apiArg.Add("monto", editDoc.Monto);
            apiArg.Add("fecha", editDoc.Fecha.ToString("yyyy-MM-dd"));

            HttpResponseMessage response = await App.httpClient.PutAsJsonAsync(
               $"/documentos", apiArg, new JsonSerializerOptions
               {
                   DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
               });
            if (!response.IsSuccessStatusCode)
            {
                EditDocInfoBar.Title = "Error";
                EditDocInfoBar.Message = "Durante la comunicacion con la API";
                EditDocInfoBar.IsOpen = true;
                EditDocInfoBar.Opacity = 1;

                timer.Tick += (sender, args) =>
                {
                    dispatcherQueue.TryEnqueue(async () =>
                    {
                        EditDocInfoBar.Opacity = 0;
                        await Task.Delay(TimeSpan.FromMilliseconds(500));
                        EditDocInfoBar.IsOpen = false;
                    });
                    timer.Stop();
                };
                timer.Start();
                return;
            }
            this.GetDocs();
            EditDocumentoBlade.IsOpen = false;
            editDoc.Reset();

            DocumentosNotification.Content = "Documento actualizado correctamente";
            DocumentosNotification.Background = AppColors.GreenBrush;
            DocumentosNotification.Show(3000);
        }

        async private void DeleteDoc_Click(object sender, RoutedEventArgs e)
        {
            var timer = new DispatcherTimer();
            timer.Interval = System.TimeSpan.FromSeconds(3);
            DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            JsonObject apiArg = new JsonObject();
            apiArg.Add("sessionHash", App.sessionHash);
            apiArg.Add("id", editDoc.Id);

            var request = new HttpRequestMessage(HttpMethod.Delete, "/documentos");
            request.Content = new StringContent(apiArg.ToJsonString(), Encoding.UTF8, "application/json");
            request.Headers.Add("X-HTTP-Method-Override", "DELETE");
            var response = await App.httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                EditDocInfoBar.Title = "Error";
                EditDocInfoBar.Message = "Durante la comunicacion con la API";
                EditDocInfoBar.IsOpen = true;
                EditDocInfoBar.Opacity = 1;

                timer.Tick += (sender, args) =>
                {
                    dispatcherQueue.TryEnqueue(async () =>
                    {
                        EditDocInfoBar.Opacity = 0;
                        await Task.Delay(TimeSpan.FromSeconds(0.5));
                        EditDocInfoBar.IsOpen = false;
                    });
                    timer.Stop();
                };
                timer.Start();
                return;
            }
            this.GetDocs();
            EditDocumentoBlade.IsOpen = false;
            editDoc.Reset();

            DocumentosNotification.Content = "Documento eliminado correctamente";
            DocumentosNotification.Show(3000);
        }

        async public void BuildHistoricChart()
        {
            HttpResponseMessage response = await App.httpClient.GetAsync(
               $"/monthly-graph?sessionHash={App.sessionHash}&nMonths=9"
               );
            if (!response.IsSuccessStatusCode)
            {
                DocumentosNotification.Content = "Error al obtener datos para Grafico Historico";
                DocumentosNotification.Show(3000);
            }
            JsonObject resObj = JsonSerializer.Deserialize<JsonObject>(response.Content.ReadAsStringAsync().Result);
            int[] gastosArray = JsonSerializer.Deserialize<int[]>(resObj["gastosDataset"].ToString());
            int[] ingresosArray = JsonSerializer.Deserialize<int[]>(resObj["ingresosDataset"].ToString());
            int[] ahorrosArray = JsonSerializer.Deserialize<int[]>(resObj["ahorrosDataset"].ToString());
            string[] labelsArray = JsonSerializer.Deserialize<string[]>(resObj["labels"].ToString());

            IEnumerable<ICartesianAxis>  LocalLabels = new List<ICartesianAxis> {
            new Axis
                {
                    Labels = labelsArray
                }
            };

            ISeries[] LocalSeries = new ISeries[]
            {
                new LineSeries<int>
                {
                    Values = gastosArray,
                    Name = "Gastos",
                    TooltipLabelFormatter =
                        (chartPoint) => $"{chartPoint.Context.Series.Name}: {chartPoint.PrimaryValue:N0}",
                    Stroke = new SolidColorPaint(SKColor.FromHsl(345f,97.8f, 35.3f)) {StrokeThickness = 3 },
                    GeometryStroke = null,
                    GeometryFill = new SolidColorPaint(SKColor.FromHsl(345f,97.8f, 35.3f)),
                    Fill =  new SolidColorPaint(SKColor.FromHsl(345f,97.8f, 35.3f).WithAlpha(50))
                },
                new LineSeries<int>
                {
                    Values = ingresosArray,
                    Name = "Ingresos",
                    TooltipLabelFormatter =
                        (chartPoint) => $"{chartPoint.Context.Series.Name}: {chartPoint.PrimaryValue:N0}",
                    Stroke =new SolidColorPaint(SKColor.FromHsl(211f,76f, 73f)) {StrokeThickness = 3 },
                    GeometryStroke =null,
                    GeometryFill = new SolidColorPaint(SKColor.FromHsl(211f,76f, 73f)),
                    Fill =  new SolidColorPaint(SKColor.FromHsl(211f,76f, 73f).WithAlpha(50))

                },
                new LineSeries<int>
                {
                    Values = ahorrosArray,
                    Name = "Ahorros",
                    TooltipLabelFormatter =
                        (chartPoint) => $"{chartPoint.Context.Series.Name}: {chartPoint.PrimaryValue:N0}",
                    Stroke = new SolidColorPaint(SKColor.FromHsl(53f,63.3f, 52f)) {StrokeThickness = 3 },
                    GeometryStroke =null,
                    GeometryFill = new SolidColorPaint(SKColor.FromHsl(53f,63.3f, 52f)),
                    Fill =  new SolidColorPaint(SKColor.FromHsl(53f,63.3f, 52f).WithAlpha(50))

                }
            };

            // Si estos se ponian en el XAML a veces se caia al salir y volver a la pagina
            // De todas maneras sino necesitamos Bind quiza sea mejor no usar
            HistoricChart.Series = LocalSeries;
            HistoricChart.XAxes = LocalLabels;
        }
    }



}
