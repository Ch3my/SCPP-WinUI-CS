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
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            InitialChecks();
        }

        async public void InitialChecks()
        {
            if (await CheckSession.CheckValid())
            {
                // Muestra solo menu LoggedIn
                ShowPrivateMenuItems();
                contentFrame.Navigate(typeof(Dashboard));
            }
            else
            {
                // Muestra solo menu LoggedOut
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
                } else
                {
                    item.Visibility = Visibility.Visible;
                }
            }

            foreach (NavigationViewItemBase item in navView.FooterMenuItems)
            {
                item.Visibility = Visibility.Collapsed;
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
                }
                else
                {
                    item.Visibility = Visibility.Visible;
                }
            }
            foreach (NavigationViewItemBase item in navView.FooterMenuItems)
            {
                item.Visibility = Visibility.Visible;
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
            Page currentPage = e.Content as Page;

            if (currentPage is LoginPage loginPage)
            {
                loginPage.UpdateMenuLevel += UpdateMenuLevel;
                loginPage.Unloaded += (s, args) =>
                {
                    // Unloaded Event no causa MemoryLeak, usarlo para desusbribirse es buena practica dice GPT
                    loginPage.UpdateMenuLevel -= UpdateMenuLevel;
                };
            }
            if (currentPage is ConfigPage configPage)
            {
                configPage.UpdateMenuLevel += UpdateMenuLevel;
                configPage.Unloaded += (s, args) =>
                {
                    // Unloaded Event no causa MemoryLeak, usarlo para desusbribirse es buena practica dice GPT
                    configPage.UpdateMenuLevel -= UpdateMenuLevel;
                };
            }
        }

        private void UpdateMenuLevel(object sender, UpdateMenuLevelEventArgs e)
        {
            if (e.TargetLevel == "I")
            {
                // Is LoggedIn
                ShowPrivateMenuItems();
            }
            if (e.TargetLevel == "O")
            {
                // Is LoggedOut
                HidePrivateMenuItems();
            }
        }

        private void navView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            Type pageType = typeof(LoginPage);
            NavigationViewItem selectedNavItem = (NavigationViewItem)sender.SelectedItem;

            if (selectedNavItem.Name == "dashboard")
            {
                pageType = typeof(Dashboard);
            }
            if (selectedNavItem.Name == "login")
            {
                pageType = typeof(LoginPage);
            }
            if (selectedNavItem.Name == "config")
            {
                pageType = typeof(ConfigPage);
            }

            _ = contentFrame.Navigate(pageType);
        }
    }
}
