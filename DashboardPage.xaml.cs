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
using System.Diagnostics;
using SCPP_WinUI_CS.PageModels;
using static SCPP_WinUI_CS.PageModels.DashboardPageViewModel;
using SCPP_WinUI_CS.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace SCPP_WinUI_CS
{
    /// <summary>
    /// Pagina donde se pueden agregar Documentos, editar, consultar y ver algunos graficos
    /// </summary>
    public sealed partial class Dashboard : Page
    {
        DashboardPageViewModel viewModel;
        // Para formatear dentro del NumberBox Repo de Ejemplo https://github.com/sonnemaf/NumberBoxTest/blob/master/NumberBoxTest/MainPage.xaml.cs
        private DecimalFormatter IntFormatter { get; } =
            new DecimalFormatter
            {
                IsGrouped = true,
                FractionDigits = 0,
                IntegerDigits = 1
            };
        public Dashboard()
        {
            this.InitializeComponent();
            viewModel = new DashboardPageViewModel();
            this.DataContext = viewModel;

            // Al iniciar setea las fecha Inicio y termino antes de llamar a API
            this.SetFechaIniTerTipoDoc();
            this.BuildHistoricChart();
            this.BuildCatGraph();
            this.GetCategorias();
            // NOTA. Esto no es awaited
            this.CreateAsync();
            // Activamos el listener a voluntar para evitar que se llame cuando no queremos
            getDocsFormFecIniInput.DateChanged += FilterCalendar_DateChanged;
            getDocsFormFecTerInput.DateChanged += FilterCalendar_DateChanged;

        }
        async public void CreateAsync()
        {
            await this.GetTipoDoc();
            this.GetDocs();

            // NOTA. Aparentemente algunos eventos quedan en Queue y aunque agregemos el event listener
            // despues de ejecutar cosas el event listener puede activarse, este codigo intenta corregir eso
            // agregando el event listener en UI Thread pero no funciono
            // Obtain the DispatcherQueue from the current window
            //DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            // Use the DispatcherQueue to reattach the event handler
            //dispatcherQueue.TryEnqueue(() => {
            //    getDocsFormTipoDocInput.SelectionChanged += TipoDocCombo_SelectionChanged;
            //});
        }
        public void SetFechaIniTerTipoDoc()
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            viewModel.FechaInicio = new DateOnly(today.Year, today.Month, 1);

            // Si es Ahorro o Ingreso Modificamos fechaInicio a ser 1 dia del Año
            if (viewModel.Fk_tipoDoc == 2 || viewModel.Fk_tipoDoc == 3)
            {
                viewModel.FechaInicio = DateOnly.FromDateTime(new DateTime(DateTime.Now.Year, 1, 1));
            }

            DateTime startOfNextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1);
            DateTime lastDayOfMonth = startOfNextMonth.AddDays(-1);
            viewModel.FechaTermino = DateOnly.FromDateTime(lastDayOfMonth.Date);

            // Tenemos que setear las fechas a los inputs a mano para evitar el double fire
            DateOnlyToDateTimeOffsetConverter converter = new DateOnlyToDateTimeOffsetConverter();
            getDocsFormFecIniInput.Date = (DateTimeOffset?)converter.Convert(viewModel.FechaInicio);
            getDocsFormFecTerInput.Date = (DateTimeOffset?)converter.Convert(viewModel.FechaTermino);

            // Resetea la categoria
            viewModel.Fk_categoria = 0;
        }
        async public void GetCategorias()
        {
            HttpResponseMessage response = await HttpRetry.ExecuteWithRetry(async () =>
            {
                return await App.httpClient.GetAsync($"/categorias?sessionHash={App.sessionHash}");
            });
            //HttpResponseMessage response = await App.httpClient.GetAsync(
            //   $"/categorias?sessionHash={App.sessionHash}"
            //   );
            if (response == null)
            {
                FileLogger.AppendToFile("Error DashBoardPage.GetCategorias(), response es null");
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                FileLogger.AppendToFile("Error DashBoardPage.GetCategorias(), API respondio: status "
                    + response.StatusCode + " " + response.ReasonPhrase);
                return;
            }
            List<Categoria> categoriaList = JsonSerializer.Deserialize<List<Categoria>>(response.Content.ReadAsStringAsync().Result);
            // Creamos Categoria Todos
            Categoria allCat = new Categoria
            {
                Id = 0,
                Descripcion = "(Todos)"
            };

            viewModel.Categorias.Clear();
            viewModel.Categorias.Add(allCat);
            foreach (Categoria c in categoriaList)
            {
                viewModel.Categorias.Add(c);
            }
            getDocsFormCategInput.SelectedIndex = 0;
        }
        async public Task GetTipoDoc()
        {
            HttpResponseMessage response = await HttpRetry.ExecuteWithRetry(async () =>
            {
                return await App.httpClient.GetAsync($"/tipo-docs?sessionHash={App.sessionHash}");
            });

            if(response == null)
            {
                FileLogger.AppendToFile("Error DashBoardPage.GetTipoDoc(), response es null");
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                FileLogger.AppendToFile("Error DashBoardPage.GetTipoDoc(), API respondio: status "
                    + response.StatusCode + " " + response.ReasonPhrase);
                return;
            }
            List<TipoDoc> tipoDocList = JsonSerializer.Deserialize<List<TipoDoc>>(response.Content.ReadAsStringAsync().Result);
            viewModel.TipoDocs.Clear();
            foreach (TipoDoc t in tipoDocList)
            {
                viewModel.TipoDocs.Add(t);
            }
            // Comenzamos con el primer elemento que correspode a Id = 1
            getDocsFormTipoDocInput.SelectedValue = 1;
        }
        async public void GetDocs()
        {
            Dictionary<string, object> urlArg = new Dictionary<string, object>();
            urlArg["sessionHash"] = App.sessionHash;
            urlArg["fechaInicio"] = viewModel.FechaInicio.ToString("yyyy-MM-dd");
            urlArg["fechaTermino"] = viewModel.FechaTermino.ToString("yyyy-MM-dd");
            urlArg["fk_tipoDoc"] = viewModel.Fk_tipoDoc;
            urlArg["searchPhrase"] = viewModel.SearchPhrase;
            if (viewModel.Fk_categoria != 0)
            {
                urlArg["fk_categoria"] = viewModel.Fk_categoria;
            }

            HelperFunctions helperfns = new HelperFunctions();
            string urlParams = helperfns.BuildUrlParamsFromDictionary(urlArg);

            HttpResponseMessage response = await App.httpClient.GetAsync(
               $"/documentos?{urlParams}"
               );
            // response.EnsureSuccessStatusCode se podria cambiar a otro tipo de control de Error
            if (!response.IsSuccessStatusCode)
            {
                DocumentosNotification.Content = "Error al consumir API GetDocs()";
                DocumentosNotification.Background = AppColors.RedBrush;
                DocumentosNotification.Show(3000);
                return;
            }
            // Toma el JSON y lo parsea a una lista de Documentos
            List<Documento> docsList = JsonSerializer.Deserialize<List<Documento>>(response.Content.ReadAsStringAsync().Result);
            viewModel.GridRows.Clear();

            // Por ahora la unica manera que el UI se refresque
            // problemas de reactividad al asignar el ObservableCollection
            foreach (Documento d in docsList)
            {
                GridRow thisRow = new GridRow();
                thisRow.Proposito = d.Proposito;
                thisRow.Monto = d.Monto.ToString("#,##0.##");
                thisRow.Fecha = d.Fecha.ToString("dd-MM-yyyy");
                thisRow.Id = d.Id;
                viewModel.GridRows.Add(thisRow);
            }

            viewModel.SumaTotalDocs = docsList.Sum(d => d.Monto).ToString("#,##0.##");
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
            apiArg.Add("fk_tipoDoc", viewModel.NewDoc.FkTipoDoc);
            apiArg.Add("proposito", viewModel.NewDoc.Proposito);
            apiArg.Add("monto", viewModel.NewDoc.Monto);
            apiArg.Add("fecha", viewModel.NewDoc.Fecha.ToString("yyyy-MM-dd"));
            if (viewModel.NewDoc.FkTipoDoc == 1)
            {
                apiArg.Add("fk_categoria", viewModel.NewDoc.FkCategoria);
            }
            else
            {
                apiArg.Add("fk_categoria", null);
            }
            if (viewModel.NewDoc.FkTipoDoc == 0)
            {
                AddDocInfoBar.Title = "Error";
                AddDocInfoBar.Message = "Debe seleccionar Tipo de Documento";
                AddDocInfoBar.IsOpen = true;
                AddDocInfoBar.Opacity = 1;
                return;
            }
            if (viewModel.NewDoc.FkTipoDoc == 1 && viewModel.NewDoc.FkCategoria == 0)
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
            viewModel.NewDoc.Reset();
            AgregarDocumentoBlade.IsOpen = false;

            DocumentosNotification.Content = "Documento Grabado correctamente";
            DocumentosNotification.Background = AppColors.GreenBrush;
            DocumentosNotification.StartBringIntoView();
            DocumentosNotification.Show(3000);
        }

        private void FilterCalendar_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            // Solo actualiza cuando nos llaman explicitamente
            // Se ejecuta en el DateChanged pero se desactivo el TwoWay binding por doble fire
            DateOnly dateOnlyValue = new DateOnly(args.NewDate.GetValueOrDefault().Year, args.NewDate.GetValueOrDefault().Month, args.NewDate.GetValueOrDefault().Day);
            if (sender.Name == "getDocsFormFecIniInput")
            {
                viewModel.FechaInicio = dateOnlyValue;
            }
            if (sender.Name == "getDocsFormFecTerInput")
            {
                viewModel.FechaTermino = dateOnlyValue;
            }
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
                viewModel.EditDoc.Reset();
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
            viewModel.EditDoc.Reset();
            // Resetea errores si hubieran
            EditDocInfoBar.Opacity = 0;
            EditDocInfoBar.IsOpen = false;

            viewModel.EditDoc.Monto = docsList[0].Monto;
            viewModel.EditDoc.Id = docsList[0].Id;
            viewModel.EditDoc.Proposito = docsList[0].Proposito;
            viewModel.EditDoc.FkTipoDoc = docsList[0].FkTipoDoc;
            viewModel.EditDoc.FkCategoria = docsList[0].FkCategoria;
            viewModel.EditDoc.Fecha = docsList[0].Fecha;
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
            apiArg.Add("id", viewModel.EditDoc.Id);
            apiArg.Add("fk_tipoDoc", viewModel.EditDoc.FkTipoDoc);
            apiArg.Add("proposito", viewModel.EditDoc.Proposito);
            apiArg.Add("monto", viewModel.EditDoc.Monto);
            apiArg.Add("fecha", viewModel.EditDoc.Fecha.ToString("yyyy-MM-dd"));
            if (viewModel.EditDoc.FkTipoDoc == 1)
            {
                apiArg.Add("fk_categoria", viewModel.EditDoc.FkCategoria);
            }
            else
            {
                apiArg.Add("fk_categoria", null);
            }

            if (viewModel.EditDoc.FkTipoDoc == 0)
            {
                EditDocInfoBar.Title = "Error";
                EditDocInfoBar.Message = "Debe seleccionar Tipo de Documento";
                EditDocInfoBar.Opacity = 1;
                EditDocInfoBar.IsOpen = true;
                return;
            }

            if (viewModel.EditDoc.FkTipoDoc == 1 && viewModel.EditDoc.FkCategoria == 0)
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
            viewModel.EditDoc.Reset();

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
            apiArg.Add("id", viewModel.EditDoc.Id);

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
            viewModel.EditDoc.Reset();

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
                    //Padding = new LiveChartsCore.Drawing.Padding {Top = (float)(HistoricChart.ActualHeight - 50)},
                    Padding = new LiveChartsCore.Drawing.Padding {Top = -50},
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

        private void TipoDocCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!getDocsFormTipoDocInput.IsEnabled)
            {
                getDocsFormTipoDocInput.IsEnabled = true;
                return;
            }
            //Desactivamos listener para operar y luego activamos
            //Evitar double fire
            getDocsFormFecIniInput.DateChanged -= FilterCalendar_DateChanged;
            getDocsFormFecTerInput.DateChanged -= FilterCalendar_DateChanged;
            SetFechaIniTerTipoDoc();
            this.GetDocs();
            getDocsFormFecIniInput.DateChanged += FilterCalendar_DateChanged;
            getDocsFormFecTerInput.DateChanged += FilterCalendar_DateChanged;
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
            viewModel.BarGraphData = JsonSerializer.Deserialize<BarGraphApiStruct>(response.Content.ReadAsStringAsync().Result);
            // aseguramos de tener integers. Desde API podrian venir doubles y se no se ejecuta el grafico porque se cae
            // TODO, creo que la DB ahora es INTEGER asi que se podria evitar este paso
            int[] dataArr = new int[viewModel.BarGraphData.Amounts.Length];
            for (int i = 0; i < viewModel.BarGraphData.Amounts.Length; i++)
            {
                dataArr[i] = (int)viewModel.BarGraphData.Amounts[i];
            }

            // Cortamos Strings a 5 Char para que se vean las etiquetas
            List<string> cutList = viewModel.BarGraphData.Labels.Select(s => s.Substring(0, Math.Min(s.Length, 5))).ToList();

            IEnumerable<ICartesianAxis> LocalLabels = new List<ICartesianAxis> {
            new Axis
                {
                    Labels = cutList,
                    //Padding = new LiveChartsCore.Drawing.Padding {Top = (float)(CategoryChart.ActualHeight  -90)},
                    Padding =  new LiveChartsCore.Drawing.Padding {Top = -50},
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
                            return $"{viewModel.BarGraphData.Labels[DataIndex]}: {chartPoint.PrimaryValue:N0}";
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
            if (viewModel.EditDoc.FkTipoDoc == 1)
            {
                EditDocCatGrid.Visibility = Visibility.Visible;
                return;
            }
            EditDocCatGrid.Visibility = Visibility.Collapsed;
        }
        private void NewDocTipoDoc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (viewModel.NewDoc.FkTipoDoc == 1)
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

        private void CategCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (!getDocsFormCategInput.IsEnabled)
            {
                // Al cargar datos desde API ejecuta trigger, cuando trigger la primera vez habilitamos
                // y volvemos, no necesitamos buscar documentos porque solo se cargo la lista de
                // categorias
                getDocsFormCategInput.IsEnabled = true;
                return;
            }
            this.GetDocs();
        }

        private void InitialApiCall_Click(object sender, RoutedEventArgs e)
        {
            this.GetCategorias();
            this.GetTipoDoc();
        }

        private void DefaultDateTipoDoc_Click(object sender, RoutedEventArgs e)
        {
            getDocsFormFecIniInput.DateChanged -= FilterCalendar_DateChanged;
            getDocsFormFecTerInput.DateChanged -= FilterCalendar_DateChanged;
            this.SetFechaIniTerTipoDoc();
            this.GetDocs();
            getDocsFormFecIniInput.DateChanged += FilterCalendar_DateChanged;
            getDocsFormFecTerInput.DateChanged += FilterCalendar_DateChanged;
        }

        private void BladeClosed(object sender, CommunityToolkit.WinUI.UI.Controls.BladeItem e)
        {
            if (e.Name == "EditDocumentoBlade")
            {
                DocsDataGrid.SelectedIndex = -1;
            }
        }

        private void ShowBarChartDetails(IChartView chart, ChartPoint point)
        {
            DataItem dataPoint = viewModel.BarGraphData.Data[(int)point.SecondaryValue];
            viewModel.Fk_categoria = dataPoint.CatId;

            // Seteamos fechas a 12 meses para ser el mismo intervalo que el grafico
            // Luego el usuario tendria que usar el boton HOME o volver a setearlas
            DateTime tmpFch = DateTime.Now.AddYears(-1);
            tmpFch = new DateTime(tmpFch.Year, tmpFch.Month, 1);
            viewModel.FechaInicio = DateOnly.FromDateTime(tmpFch);

            DateOnlyToDateTimeOffsetConverter converter = new DateOnlyToDateTimeOffsetConverter();
            getDocsFormFecIniInput.Date = (DateTimeOffset?)converter.Convert(viewModel.FechaInicio);
        }
    }
}
