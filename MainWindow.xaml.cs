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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.AccessControl;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SCPP_WinUI_CS
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            // Siempre comenzamos en el DashBoard
            //contentFrame.Navigate(typeof(Dashboard));
            navView.SelectedItem = navView.MenuItems[0];
        }


        private void navView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            // Cuando presionan Algo en NAV llega aqui, es como routes de la pantalla principal
            // MainWindow es como layout y cada pagina es el contenido de las paginas

            // Esta logica podria existir en un archivo propio?? No se como se haria xD

            Type pageType = typeof(Dashboard);
            NavigationViewItem selectedNavItem = (NavigationViewItem)args.SelectedItem;

            if (selectedNavItem.Name == "dashboard")
            {
                pageType = typeof(Dashboard);
            }
            if (selectedNavItem.Name == "NavItem_1")
            {
                pageType = typeof(BlankPage1);
            }

            _ = contentFrame.Navigate(pageType);
        }
    }
}
