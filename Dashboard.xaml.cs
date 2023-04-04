// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Xml.Linq;
using Windows.Globalization;
using Windows.Globalization.NumberFormatting;

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

        // This class implements INotifyPropertyChanged
        // to support one-way and two-way bindings
        // (such that the UI element updates when the source
        // has been changed dynamically)
        // Usar struct con la interfaz no funciono. Pero el mismo
        // codigo con una clase si, asi que cambiamos a clase y copiamos codigo
        // de ejemplo de Microsoft
        // https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/how-to-implement-property-change-notification?view=netframeworkdesktop-4.8
        public class GetDocsForm : INotifyPropertyChanged
        {
            private string _searchPhrase;
            private int _fk_tipoDoc;
            private int _fk_categoria;
            // Declare the event
            public event PropertyChangedEventHandler PropertyChanged;

            public GetDocsForm()
            {
            }
            public int fk_tipoDoc
            {
                get { return _fk_tipoDoc; }
                set
                {
                    // Al parecer el Combobox se resetea cada vez que se setea y causa loop
                    if (_fk_tipoDoc == value)
                    {
                        return;
                    }
                    _fk_tipoDoc = value;
                    OnPropertyChanged();
                }
            }
            public int fk_categoria
            {
                get { return _fk_categoria; }
                set
                {
                    // Al parecer el Combobox se resetea cada vez que se setea y causa loop
                    if (_fk_categoria == value)
                    {
                        return;
                    }
                    _fk_categoria = value;
                    OnPropertyChanged();
                }
            }
            public string searchPhrase
            {
                get { return _searchPhrase; }
                set
                {
                    _searchPhrase = value;
                    OnPropertyChanged();
                }
            }

            // Create the OnPropertyChanged method to raise the event
            // The calling member's name will be used as the parameter.
            protected void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        public GetDocsForm getDocsForm = new GetDocsForm
        {
            fk_tipoDoc = 1,
            searchPhrase = ""
        };
        public struct GridRow
        {
            public string Proposito { get; set; }
            public string Monto { get; set; }
            public string Fecha { get; set; }

        }
        ObservableCollection<Categoria> categorias = new ObservableCollection<Categoria>();
        ObservableCollection<TipoDoc> tipoDocs = new ObservableCollection<TipoDoc>();

        public ObservableCollection<GridRow> gridRows { get; set; } = new();

        public Dashboard()
        {
            this.InitializeComponent();
            this.GetCategorias();
            this.GetTipoDoc();
            this.GetDocs();
        }
        async public void GetCategorias()
        {
            HttpResponseMessage response = await App.httpClient.GetAsync(
               $"/categorias?sessionHash=v03ex42bdrqrilrrybjgk"
               );
            response.EnsureSuccessStatusCode();
            List<Categoria> categoriaList = JsonSerializer.Deserialize<List<Categoria>>(response.Content.ReadAsStringAsync().Result);
            foreach (Categoria c in categoriaList)
            {
                categorias.Add(c);
            }
        }
        async public void GetTipoDoc()
        {
            HttpResponseMessage response = await App.httpClient.GetAsync(
               $"/tipo-docs?sessionHash=v03ex42bdrqrilrrybjgk"
               );
            response.EnsureSuccessStatusCode();
            List<TipoDoc> tipoDocList = JsonSerializer.Deserialize<List<TipoDoc>>(response.Content.ReadAsStringAsync().Result);
            foreach (TipoDoc t in tipoDocList)
            {
                tipoDocs.Add(t);
            }
        }
        async public void GetDocs()
        {
            HttpResponseMessage response = await App.httpClient.GetAsync(
               $"/documentos?fk_tipoDoc={getDocsForm.fk_tipoDoc}&sessionHash=v03ex42bdrqrilrrybjgk"
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

        private void SaveNewDoc_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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

            Documento newDoc = new Documento();
            newDoc.Monto = Convert.ToInt32(montoInput.Value);
            newDoc.Proposito = propositoInput.Text;
            newDoc.Fecha = DateOnly.FromDateTime(newDocFechaInput.Date.Value.DateTime);
        }
    }



}
