// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SCPP_WinUI_CS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Dashboard : Page
    {
        struct GetDocsForm
        {
            public int fk_tipoDoc { get; set; }
        }
        GetDocsForm getDocsForm = new GetDocsForm
        {
            fk_tipoDoc = 1
        };
        public struct GridRow
        {
            public string Proposito { get; set; }
            public string Monto { get; set; }

        }

        public ObservableCollection<GridRow> gridRows { get; set; } = new();

        public Dashboard()
        {
            this.InitializeComponent();
            this.GetDocs();
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
    }



}
