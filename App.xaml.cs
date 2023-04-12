// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using SCPP_WinUI_CS;
using System.Threading.Tasks;
using System.Text.Json.Nodes;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SCPP_WinUI_CS
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static HttpClient httpClient = new HttpClient();
        public static string sessionHash = "";
        private Window m_window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            // Chequeamos si existe archivo
            if (Config.CheckConfigFile())
            {
                LoadConfig();
            }
            if (!Config.CheckConfigFile())
            {
                Config.CreateConfigFile();
            }

            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }

        static public void LoadConfig()
        {
            JsonObject currentConfig = Config.GetConfig();

            if (!Uri.IsWellFormedUriString(currentConfig["apiPrefix"].ToString(), UriKind.Absolute))
            {
                return;
            }
            sessionHash = currentConfig["sessionHash"].ToString();

            // Microsoft recomienda un Clte por App?
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(currentConfig["apiPrefix"].ToString());
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}
