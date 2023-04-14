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
using CommunityToolkit.WinUI.Helpers;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView.VisualElements;
using static System.Net.Mime.MediaTypeNames;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace SCPP_WinUI_CS
{
    /// <summary>
    /// Pagina donde se pueden agregar Documentos, editar, consultar y ver algunos graficos
    /// </summary>
    public sealed partial class Dashboard : Page
    {
        public Dashboard()
        {
            this.InitializeComponent();
            // Al iniciar setea las fecha Inicio y termino antes de llamar a API
            this.SetFechaIniTerTipoDoc();
            this.GetCategorias();
            this.GetTipoDoc();
            this.GetDocs();
            this.BuildHistoricChart();
            this.BuildCatGraph();
        }
        public void SetFechaIniTerTipoDoc()
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            getDocsForm.fechaInicio = new DateOnly(today.Year, today.Month, 1);

            // Si es Ahorro o Ingreso Modificamos fechaInicio a ser 1 dia del Año
            if (getDocsForm.fk_tipoDoc == 2 || getDocsForm.fk_tipoDoc == 3)
            {
                getDocsForm.fechaInicio = DateOnly.FromDateTime(new DateTime(DateTime.Now.Year, 1, 1));
            }

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

            SumaTotalDocs.Value = docsList.Sum(d => d.Monto).ToString("#,##0.##");
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
            // resetea Errores si hubieran
            AddDocInfoBar.IsOpen = false;
            AddDocInfoBar.Opacity = 0;
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

            JsonObject apiArg = new JsonObject();
            apiArg.Add("sessionHash", App.sessionHash);
            apiArg.Add("fk_tipoDoc", newDoc.FkTipoDoc);
            apiArg.Add("proposito", newDoc.Proposito);
            apiArg.Add("monto", newDoc.Monto);
            apiArg.Add("fecha", newDoc.Fecha.ToString("yyyy-MM-dd"));
            if (newDoc.FkTipoDoc == 1)
            {
                apiArg.Add("fk_categoria", newDoc.FkCategoria);
            }
            else
            {
                apiArg.Add("fk_categoria", null);
            }
            if (newDoc.FkTipoDoc == 0)
            {
                AddDocInfoBar.Title = "Error";
                AddDocInfoBar.Message = "Debe seleccionar Tipo de Documento";
                AddDocInfoBar.IsOpen = true;
                AddDocInfoBar.Opacity = 1;
                return;
            }
            if (newDoc.FkTipoDoc == 1 && newDoc.FkCategoria == 0)
            {
                AddDocInfoBar.Title = "Error";
                AddDocInfoBar.Message = "Debe seleccionar una categoria";
                AddDocInfoBar.IsOpen = true;
                AddDocInfoBar.Opacity = 1;
                return;
            }

            HttpResponseMessage response = await App.httpClient.PostAsJsonAsync(
           $"/documentos", apiArg);

            if (!response.IsSuccessStatusCode)
            {
                DocumentosNotification.Content = "Durante la comunicacion con la API";
                DocumentosNotification.Background = AppColors.RedBrush;
                DocumentosNotification.Show(3000);
                return;
            }
            this.GetDocs();
            this.BuildCatGraph();
            this.BuildHistoricChart();
            newDoc.Reset();

            DocumentosNotification.Content = "Documento Grabado correctamente";
            DocumentosNotification.Background = AppColors.GreenBrush;
            DocumentosNotification.StartBringIntoView();
            DocumentosNotification.Show(3000);
        }

        private void FilterCalendar_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            this.GetDocs();
        }

        private void RefreshDocs_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            this.GetDocs();
            this.BuildCatGraph();
            this.BuildHistoricChart();
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
                DocumentosNotification.Content = "Durante la comunicacion con la API";
                DocumentosNotification.Background = AppColors.RedBrush;
                DocumentosNotification.Show(3000);
                return;
            }
            List<Documento> docsList = JsonSerializer.Deserialize<List<Documento>>(response.Content.ReadAsStringAsync().Result);
            editDoc.Reset();
            // Resetea errores si hubieran
            EditDocInfoBar.Opacity = 0;
            EditDocInfoBar.IsOpen = false;

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
            apiArg.Add("fk_tipoDoc", editDoc.FkTipoDoc);
            apiArg.Add("proposito", editDoc.Proposito);
            apiArg.Add("monto", editDoc.Monto);
            apiArg.Add("fecha", editDoc.Fecha.ToString("yyyy-MM-dd"));
            if (editDoc.FkTipoDoc == 1)
            {
                apiArg.Add("fk_categoria", editDoc.FkCategoria);
            }
            else
            {
                apiArg.Add("fk_categoria", null);
            }

            if (editDoc.FkTipoDoc == 0)
            {
                EditDocInfoBar.Title = "Error";
                EditDocInfoBar.Message = "Debe seleccionar Tipo de Documento";
                EditDocInfoBar.Opacity = 1;
                EditDocInfoBar.IsOpen = true;
                return;
            }

            if (editDoc.FkTipoDoc == 1 && editDoc.FkCategoria == 0)
            {
                EditDocInfoBar.Title = "Error";
                EditDocInfoBar.Message = "Debe seleccionar una categoria";
                EditDocInfoBar.Opacity = 1;
                EditDocInfoBar.IsOpen = true;
                return;
            }

            HttpResponseMessage response = await App.httpClient.PutAsJsonAsync(
               $"/documentos", apiArg);
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
            this.BuildCatGraph();
            this.BuildHistoricChart();
            EditDocumentoBlade.IsOpen = false;
            editDoc.Reset();

            DocumentosNotification.Content = "Documento actualizado correctamente";
            DocumentosNotification.Background = AppColors.GreenBrush;
            DocumentosNotification.StartBringIntoView();
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
            this.BuildCatGraph();
            this.BuildHistoricChart();
            EditDocumentoBlade.IsOpen = false;
            editDoc.Reset();

            DocumentosNotification.Content = "Documento eliminado correctamente";
            DocumentosNotification.Background = AppColors.GreenBrush;
            DocumentosNotification.StartBringIntoView();
            DocumentosNotification.Show(3000);
        }

        async public void BuildHistoricChart()
        {
            HttpResponseMessage response = await App.httpClient.GetAsync(
               $"/monthly-graph?sessionHash={App.sessionHash}&nMonths=12"
               );
            if (!response.IsSuccessStatusCode)
            {
                DocumentosNotification.Content = "Error al obtener datos para Grafico Historico";
                DocumentosNotification.Background = AppColors.RedBrush;
                DocumentosNotification.Show(3000);
                return;
            }
            JsonObject resObj = JsonSerializer.Deserialize<JsonObject>(response.Content.ReadAsStringAsync().Result);
            int[] gastosArray = JsonSerializer.Deserialize<int[]>(resObj["gastosDataset"].ToString());
            int[] ingresosArray = JsonSerializer.Deserialize<int[]>(resObj["ingresosDataset"].ToString());
            int[] ahorrosArray = JsonSerializer.Deserialize<int[]>(resObj["ahorrosDataset"].ToString());
            // Cortamos solo los ultimos 5 Char que son suficientes y ahorramos espacio
            List<string> labelsList = JsonSerializer.Deserialize<List<string>>(resObj["labels"].ToString());
            List<string> cutList = labelsList.Select(s => s.Substring(Math.Max(0, s.Length - 5))).ToList();

            // No logre poder leer bien los colores del sistema para aplicar
            IEnumerable<ICartesianAxis> LocalLabels = new List<ICartesianAxis> {
            new Axis
                {
                    Labels = cutList,
                    Padding = new LiveChartsCore.Drawing.Padding {Top = 0},
                    LabelsPaint = new SolidColorPaint
                    {
                        Color =  SKColors.Gray
                    },
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

        private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetFechaIniTerTipoDoc();
            this.GetDocs();
        }

        async public void BuildCatGraph()
        {
            HttpResponseMessage response = await App.httpClient.GetAsync(
            $"/expenses-by-category?sessionHash={App.sessionHash}"
            );
            if (!response.IsSuccessStatusCode)
            {
                DocumentosNotification.Content = "Error al obtener datos para Grafico Categoria";
                DocumentosNotification.Background = AppColors.RedBrush;
                DocumentosNotification.Show(3000);
                return;
            }
            JsonObject resObj = JsonSerializer.Deserialize<JsonObject>(response.Content.ReadAsStringAsync().Result);
            // aseguramos de tener integers. Desde API podrian venir doubles y se no se ejecuta el grafico porque se cae
            double[] tmpData = JsonSerializer.Deserialize<double[]>(resObj["amounts"].ToString());
            int[] dataArr = new int[tmpData.Length];
            for (int i = 0; i < tmpData.Length; i++)
            {
                dataArr[i] = (int)tmpData[i];
            }

            // Cortamos Strings a 5 Char para que se vean las etiquetas
            List<string> labelsList = JsonSerializer.Deserialize<List<string>>(resObj["labels"].ToString());
            List<string> cutList = labelsList.Select(s => s.Substring(0, Math.Min(s.Length, 5))).ToList();

            IEnumerable<ICartesianAxis> LocalLabels = new List<ICartesianAxis> {
            new Axis
                {
                    Labels = cutList,
                    Padding = new LiveChartsCore.Drawing.Padding {Top = -120},
                    //LabelsRotation = -20,
                    TextSize = 14,
                    //LabelsAlignment = LiveChartsCore.Drawing.Align.End,
                    LabelsPaint = new SolidColorPaint
                    {
                        Color =  SKColors.Gray,
                    },
                }
            };

            ISeries[] LocalSeries = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Values = dataArr,
                    Name = "Categoria",
                    TooltipLabelFormatter = (chartPoint) => {
                        // Buscamos el Index a Mano y nos traemos su Label. Aparentemente no se puede hacer
                        // Automatico
                        int DataIndex = Array.FindIndex(dataArr, x => x == ((int)chartPoint.PrimaryValue));
                        if(DataIndex != -1)
                        {
                            return $"{labelsList[DataIndex]}: {chartPoint.PrimaryValue:N0}";
                        }
                            return  $"{chartPoint.Context.Series.Name}: {chartPoint.PrimaryValue:N0}";
                        },
                    Stroke = null,
                    Fill =  new SolidColorPaint(SKColor.FromHsl(91f,40f, 66f)),
                },
            };

            CategoryChart.Series = LocalSeries;
            CategoryChart.XAxes = LocalLabels;
        }

        private void EditDocTipoDoc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (editDoc.FkTipoDoc == 1)
            {
                EditDocCatGrid.Visibility = Visibility.Visible;
                return;
            }
            EditDocCatGrid.Visibility = Visibility.Collapsed;
        }
        private void NewDocTipoDoc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (newDoc.FkTipoDoc == 1)
            {
                NewDocCatGrid.Visibility = Visibility.Visible;
                return;
            }
            NewDocCatGrid.Visibility = Visibility.Collapsed;
        }

        private void DataGridDocsBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.GetDocs();
        }
    }
}
