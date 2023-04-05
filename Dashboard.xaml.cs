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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SCPP_WinUI_CS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Dashboard : Page
    {
        // Para formatear dentro del NumberBox Repo de Ejemplo https://github.com/sonnemaf/NumberBoxTest/blob/master/NumberBoxTest/MainPage.xaml.cs
        private DecimalFormatter IntFormatter { get; } =
            new DecimalFormatter(new[] { "nl-NL" }, "NL")
            {
                IsGrouped = true,
                FractionDigits = 0,
                NumberRounder = new IncrementNumberRounder(),
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

        public Dashboard()
        {
            this.InitializeComponent();
            // Al iniciar setea las fecha Inicio y termino antes de llamar a API
            this.SetFechaIniTerTipoDoc();
            int test = -1;
            this.GetCategorias();
            this.GetTipoDoc();
            this.GetDocs();

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
               $"/categorias?sessionHash=v03ex42bdrqrilrrybjgk"
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
               $"/tipo-docs?sessionHash=v03ex42bdrqrilrrybjgk"
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
               $"/documentos?sessionHash=v03ex42bdrqrilrrybjgk&{urlParams}"
               );
            // response.EnsureSuccessStatusCode se podria cambiar a otro tipo de control de Error
            response.EnsureSuccessStatusCode();
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
            test.Add("sessionHash", "v03ex42bdrqrilrrybjgk");
            test.Add("fk_categoria", newDoc.FkCategoria);
            test.Add("fk_tipoDoc", newDoc.FkTipoDoc);
            test.Add("proposito", newDoc.Proposito);
            test.Add("monto", newDoc.Monto);
            test.Add("fecha", newDoc.Fecha.ToString("yyyy-MM-dd"));

            HttpResponseMessage response = await App.httpClient.PostAsJsonAsync(
               $"/documentos",
               test,
               new JsonSerializerOptions
               {
                   DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
               }
               );

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            if (!response.IsSuccessStatusCode)
            {
                AddDocInfoBar.Title = "Error";
                AddDocInfoBar.Message = "Durante la comunicacion con la API";
                AddDocInfoBar.IsOpen = true;
                AddDocInfoBar.Opacity = 1;

                // Handle the Tick event to update the UI
                timer.Tick += (sender, args) =>
                {
                    dispatcherQueue.TryEnqueue(() =>
                    {
                        // Update the UI component here
                        AddDocInfoBar.Opacity = 0;
                        AddDocInfoBar.IsOpen = false;
                    });

                    // Stop the timer after the update is complete
                    timer.Stop();
                };

                // Start the timer
                timer.Start();
                return;
            }

            this.GetDocs();
            newDoc.Reset();
            AddDocInfoBar.Title = "Exito!";
            AddDocInfoBar.Message = "Documento Grabado correctamente";
            AddDocInfoBar.IsOpen = true;
            AddDocInfoBar.Opacity = 1;

            // Creamos una manera de poder modificar la UI desde otro Thread 
            // el Otro Thread espera 3 segundos antes de ejecutarse
            // Handle the Tick event to update the UI
            timer.Tick += (sender, args) =>
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    // Update the UI component here
                    AddDocInfoBar.Opacity = 0;
                    AddDocInfoBar.IsOpen = false;
                });

                // Stop the timer after the update is complete
                timer.Stop();
            };

            // Start the timer
            timer.Start();
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
            GridRow selectedRow = (GridRow)e.AddedItems[0];

            Dictionary<string, object> urlArg = new Dictionary<string, object>();
            urlArg["sessionHash"] = "v03ex42bdrqrilrrybjgk";
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
            editDoc.Monto = docsList[0].Monto;
            editDoc.Id = docsList[0].Id;
            editDoc.Proposito = docsList[0].Proposito;
            editDoc.FkTipoDoc = docsList[0].FkTipoDoc;
            editDoc.FkCategoria= docsList[0].FkCategoria;
            editDoc.Fecha= docsList[0].Fecha;
            EditDocumentoBlade.IsOpen = true;
        }

        private void SaveEditDoc_Click(object sender, RoutedEventArgs e)
        {

        }
    }



}
