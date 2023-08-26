using LiveChartsCore.VisualElements;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using SCPP_WinUI_CS.Models;
using SCPP_WinUI_CS.PageModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT.Interop;
using static SCPP_WinUI_CS.Dashboard;
using static SCPP_WinUI_CS.PageModels.AssetsPageViewModel;
using static System.Net.Mime.MediaTypeNames;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SCPP_WinUI_CS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AssetsPage : Page
    {
        private AssetsPageViewModel viewModel;

        private double scaleFactor = 1.0;
        private Point lastPanPosition;

        public AssetsPage()
        {
            this.InitializeComponent();
            viewModel = new AssetsPageViewModel();
            this.DataContext = viewModel;
            this.GetData();
            this.GetCategorias();

            imageControl.ManipulationMode = ManipulationModes.Scale | ManipulationModes.TranslateX | ManipulationModes.TranslateY;
        }

        async public void GetData()
        {
            HelperFunctions helperfns = new HelperFunctions();
            string urlParams = helperfns.BuildUrlParamsFromStruct(viewModel.getAssetsForm);

            HttpResponseMessage response = await App.httpClient.GetAsync(
               $"/assets?sessionHash={App.sessionHash}&{urlParams}"
               );
            // response.EnsureSuccessStatusCode se podria cambiar a otro tipo de control de Error
            if (!response.IsSuccessStatusCode)
            {
                return;
            }
            List<Asset> assetsList =
                JsonSerializer.Deserialize<List<Asset>>(response.Content.ReadAsStringAsync().Result);
            viewModel.assetRows.Clear();
            foreach (Asset a in assetsList)
            {
                AssetRow thisRow = new AssetRow();
                thisRow.Descripcion = a.Descripcion;
                thisRow.Fecha = a.Fecha.ToString("dd-MM-yyyy");
                thisRow.Id = a.Id;
                viewModel.assetRows.Add(thisRow);
            }
        }

        async private void Assets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                //return;
            }
            AssetRow selectedRow = (AssetRow)e.AddedItems[0];
            Dictionary<string, object> urlArg = new Dictionary<string, object>();
            urlArg["sessionHash"] = App.sessionHash;
            urlArg["id[]"] = selectedRow.Id;

            HelperFunctions helperfns = new HelperFunctions();
            string urlParams = helperfns.BuildUrlParamsFromDictionary(urlArg);

            HttpResponseMessage response = await App.httpClient.GetAsync(
               $"/assets?{urlParams}"
               );
            if (!response.IsSuccessStatusCode)
            {
                return;
            }
            List<Asset> assetList = JsonSerializer.Deserialize<List<Asset>>(response.Content.ReadAsStringAsync().Result);
            viewModel.editAsset = assetList.FirstOrDefault();
            // Quitamos cabecera data:image/png;base64 porque no la necesitamos
            // la guardamos en DB porque puede ser util
            string base64Image = viewModel.editAsset.AssetData.Split(',')[1];
            // Decode the Base64 string to a byte array
            byte[] imageBytes = Convert.FromBase64String(base64Image);
            BitmapImage bitmapImage = new BitmapImage();
            using (var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream())
            {
                stream.WriteAsync(imageBytes.AsBuffer()).GetResults();
                stream.Seek(0);
                bitmapImage.SetSource(stream);
            }

            // Assign the BitmapImage to the Source property of the Image control
            imageControl.Source = bitmapImage;

            // Abrimos Blade para mostrar info adicional que podria tener
            EditAssetBlade.IsOpen = true;
        }
        private void Image_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // Apply zoom
            scaleFactor *= e.Delta.Scale;

            // Apply translation
            var newPosition = new Point(lastPanPosition.X + e.Delta.Translation.X, lastPanPosition.Y + e.Delta.Translation.Y);

            // Apply transformations
            imageControl.RenderTransform = new CompositeTransform
            {
                ScaleX = scaleFactor,
                ScaleY = scaleFactor,
                TranslateX = newPosition.X,
                TranslateY = newPosition.Y
            };

            lastPanPosition = newPosition;
        }

        private void Image_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var transform = (CompositeTransform)imageControl.RenderTransform;
            lastPanPosition = new Point(transform.TranslateX, transform.TranslateY);
        }
        async public void GetCategorias()
        {
            HttpResponseMessage response = await App.httpClient.GetAsync(
               $"/categorias?sessionHash={App.sessionHash}"
               );
            response.EnsureSuccessStatusCode();
            List<Categoria> categoriaList = JsonSerializer.Deserialize<List<Categoria>>(response.Content.ReadAsStringAsync().Result);
            viewModel.Categorias.Clear();
            foreach (Categoria c in categoriaList)
            {
                viewModel.Categorias.Add(c);
            }
        }

        private void AddAsset_Click(object sender, RoutedEventArgs e)
        {
            AddAssetBlade.IsOpen = true;
        }

        private void RefreshAssets_Click(object sender, RoutedEventArgs e)
        {
            this.GetData();
        }

        private void SaveNewAsset_Click(object sender, RoutedEventArgs e)
        {
            viewModel.newAsset.Reset();
        }

        private async void PickAFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Se hizo asi, el ejemplo no funcionaba, En el futuro se podria cambiar
            // a un objeto App.Window privado como estaba cuando se inicio el proyecto
            FileOpenPicker fileOpenPicker = new()
            {
                ViewMode = PickerViewMode.Thumbnail,
                FileTypeFilter = { ".jpg", ".jpeg", ".png"},
            };

            nint windowHandle = WindowNative.GetWindowHandle(App.Window);
            InitializeWithWindow.Initialize(fileOpenPicker, windowHandle);

            StorageFile selectedFile = await fileOpenPicker.PickSingleFileAsync();

            if (selectedFile != null)
            {
                // Mostrar imagen
                using (IRandomAccessStream fileStream = await selectedFile.OpenAsync(FileAccessMode.Read))
                {
                    BitmapImage image = new BitmapImage();
                    await image.SetSourceAsync(fileStream);
                    imageControl.Source = image;
                }
                // guardar base64 en variable por si guardan
                viewModel.newAsset.AssetData = await ConvertFileToBase64(selectedFile);
            }
        }
        private async Task<string> ConvertFileToBase64(StorageFile file)
        {
            using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
            {
                using (DataReader reader = new DataReader(fileStream))
                {
                    await reader.LoadAsync((uint)fileStream.Size);
                    byte[] byteArray = new byte[fileStream.Size];
                    reader.ReadBytes(byteArray);
                    return Convert.ToBase64String(byteArray);
                }
            }
        }
    }
}
