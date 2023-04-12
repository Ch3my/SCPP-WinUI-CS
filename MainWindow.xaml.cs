// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI.Controls;
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
            //navView.SelectedItem = navView.MenuItems[0];
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            InitialChecks();
        }

        async public void InitialChecks()
        {
            if(await CheckSession.CheckValid())
            {
                contentFrame.Navigate(typeof(Dashboard));
            }
            else
            {
                HidePrivateMenuItems();
                contentFrame.Navigate(typeof(LoginPage));
            }
        }

        public void HidePrivateMenuItems()
        {
            foreach (NavigationViewItemBase item in navView.MenuItems)
            {
                if (item.Name != "login")
                {
                    item.Visibility = Visibility.Collapsed;
                }
            }
        }
        public void ShowPrivateMenuItems()
        {
            // En teoria la pantalla de Login es la unica que solamente es publica
            // ocultamos eso y mostramos todo lo demas
            foreach (NavigationViewItemBase item in navView.MenuItems)
            {
                if (item.Name == "login")
                {
                    item.Visibility = Visibility.Collapsed;
                } else
                {
                    item.Visibility = Visibility.Visible;
                }
            }
        }
        private void SetSelectedNavItemByName(string name)
        {
            // Loop through each menu item and find the item with the matching name
            foreach (NavigationViewItemBase item in navView.MenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Name == name)
                {
                    // Set the selected item to the item with the matching name
                    navView.SelectedItem = navItem;
                    break;
                }
            }
        }

        private void contentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            // Este evento se llama cuando se hace click y cuando se llama Frame.Navigate directamente
            // al llamar Frame.Navigate no pasa por el evento navView_SelectionChanged pero si por aca
            if (e.SourcePageType.Name == "Dashboard")
            {
                // Nos aseguramos de mostrar las opciones del menu que corresponden
                ShowPrivateMenuItems();
            }
            // Cuando llaman Frame.Navigate el indicador del menu actual no se actualiza
            // aqui lo seteamos si es que son diferentes
            // Esta linea se ejecuta 2 veces cuando el usuario hace clic en el menu. Si se pudiera verificar
            // antes de ejecutar estariamos evitando repeticion (se ejecuta el cambio de indicador automatico y luego manual aca)
            SetSelectedNavItemByName(e.SourcePageType.Name.ToLower());

            // La navegacion como tal se ejecuta en navView_ItemInvoked asi que no necesitamos llamar
            // a contentFrame.Navigate aqui
        }

        private void navView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            Type pageType = typeof(LoginPage);
            NavigationViewItem selectedNavItem = (NavigationViewItem)sender.SelectedItem;

            if (selectedNavItem.Name == "dashboard")
            {
                pageType = typeof(Dashboard);
            }
            if (selectedNavItem.Name == "NavItem_1")
            {
                pageType = typeof(BlankPage1);
            }
            if (selectedNavItem.Name == "login")
            {
                pageType = typeof(LoginPage);
            }

            _ = contentFrame.Navigate(pageType);
        }
    }
}
