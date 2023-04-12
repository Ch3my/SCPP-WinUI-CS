// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;
using System.Text.Json;
using System.Text.Json.Nodes;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Notifications;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SCPP_WinUI_CS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            this.InitializeComponent();
        }

        async private void DoLogin(object sender, RoutedEventArgs e)
        {
            HttpResponseMessage response;
            JsonObject loginArgs = new JsonObject();
            loginArgs.Add("username", UserTextBox.Text);
            loginArgs.Add("password", PassBox.Password);

            try
            {
                response = await App.httpClient.PostAsJsonAsync(
                   $"/login", loginArgs);
            }
            catch (Exception ex) {
                Notification.Content = "Error de comunicacion con la API";
                Notification.Background = AppColors.RedBrush;
                Notification.Show(3000);
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                Notification.Content = "Error al Iniciar sesion";
                Notification.Show(3000);
                return;
            }

            JsonObject resObj = JsonSerializer.Deserialize<JsonObject>(response.Content.ReadAsStringAsync().Result);

            if (resObj.ContainsKey("hasErrors"))
            {
                Notification.Content = "Error al Iniciar sesion. Verifica la contraseña";
                Notification.Show(3000);
                return;
            }

            if (resObj.TryGetPropertyValue("sessionHash", out JsonNode sessionHashElement))
            {
                // Obtenemos ApiPrefix para no eliminarlo al sobreescribir la configuracion
                JsonObject currentConfig = Config.GetConfig();
                string apiPrefix = currentConfig["apiPrefix"].ToString();

                string sessionHash = sessionHashElement.ToString();
                JsonObject updateConfig = new JsonObject();
                updateConfig.Add("sessionHash", sessionHash);
                updateConfig.Add("apiPrefix", apiPrefix);

                // Guarda config y navegamos donde es necesario, Actualizamos Opciones del Menu
                Config.UpdateConfigFile(updateConfig);

                Frame.Navigate(typeof(Dashboard));
            }
        }

        private void ShowConfig(object sender, RoutedEventArgs e)
        {
            JsonObject currentConfig = Config.GetConfig();
            ConfigApiPrefixTextBox.Text = currentConfig["apiPrefix"].ToString();
            ConfigSessionHashTextBox.Text = currentConfig["sessionHash"].ToString();
            ConfigBlade.IsOpen = true;
        }
        private void SaveConfig(object sender, RoutedEventArgs e)
        {
            JsonObject updateConfig = new JsonObject();
            updateConfig.Add("sessionHash", ConfigSessionHashTextBox.Text);
            updateConfig.Add("apiPrefix", ConfigApiPrefixTextBox.Text);

            if (Config.UpdateConfigFile(updateConfig))
            {
                Notification.Content = "Configuracion actualizada correctamente";
                Notification.Show(3000);
            }
        }
    }
}
