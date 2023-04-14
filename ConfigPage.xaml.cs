// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Net.Http.Json;
using Windows.UI.Notifications;
using System.Net.Http;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SCPP_WinUI_CS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ConfigPage : Page
    {
        readonly static string filePath = Path.Combine(Directory.GetCurrentDirectory(), "localstorage.json");
        public event EventHandler<UpdateMenuLevelEventArgs> UpdateMenuLevel;

        public ConfigPage()
        {
            this.InitializeComponent();
        }

        public static bool CheckConfigFile()
        {
            if (File.Exists(filePath))
            {
                return true;
            }
            return false;
        }

        public static bool CreateConfigFile()
        {
            JsonObject configFileContent = new JsonObject();
            configFileContent.Add("sessionHash", "");
            configFileContent.Add("apiPrefix", "");

            File.WriteAllText(filePath, JsonSerializer.Serialize(configFileContent));

            return true;
        }

        public static bool UpdateConfigFile(JsonObject fileContent)
        {
            File.WriteAllText(filePath, JsonSerializer.Serialize(fileContent));
            App.LoadConfig();
            return true;
        }

        public static JsonObject GetConfig()
        {
            string json = File.ReadAllText(filePath);
            // Parse the JSON string into a JsonObject
            JsonObject jsonObject = JsonSerializer.Deserialize<JsonObject>(json);

            return jsonObject;
        }

        private void ConfigOpts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }
            TextBlock selectedItem = (TextBlock)e.AddedItems[0];
            if (selectedItem.Tag.ToString() == "logout")
            {
                DoLogout();
            }
        }
        async public void DoLogout()
        {
            HttpResponseMessage response;
            JsonObject apiArgs = new JsonObject();
            apiArgs.Add("sessionHash", App.sessionHash);

            try
            {
                response = await App.httpClient.PostAsJsonAsync(
                   $"/logout", apiArgs);
            }
            catch (Exception ex)
            {
                Notification.Content = "Error de comunicacion con la API";
                Notification.Background = AppColors.RedBrush;
                Notification.Show(3000);
                FileLogger.AppendToFile(ex.Message);
                return;
            }
            if (!response.IsSuccessStatusCode)
            {
                Notification.Content = "Error al cerrar sesion";
                Notification.Show(3000);
                return;
            }

            JsonObject resObj = JsonSerializer.Deserialize<JsonObject>(response.Content.ReadAsStringAsync().Result);

            if (resObj.ContainsKey("hasErrors"))
            {
                Notification.Content = "Api respondio con Errores";
                Notification.Show(3000);
                return;
            }
            Frame.Navigate(typeof(LoginPage));
            // O de Out
            UpdateMenuLevel?.Invoke(this, new UpdateMenuLevelEventArgs("O"));

        }
    }
}
