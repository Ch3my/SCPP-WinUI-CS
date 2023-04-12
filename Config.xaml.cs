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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SCPP_WinUI_CS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Config : Page
    {
        readonly static string filePath = Path.Combine(Directory.GetCurrentDirectory(), "localstorage.json");
        public Config()
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
    }
}
