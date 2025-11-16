using Avalonia.Controls;
using Avalonia.Interactivity;
using myApp.Services;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia;
using System;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Threading;
using System.Diagnostics;
using System.Collections.Generic;

namespace myApp
{
    public partial class MainWindow : Window
    {
        private readonly DashboardView _dashboardView = new DashboardView();
        private readonly SettingsView _settingsView = new SettingsView();
        private readonly ModelsView _modelsView = new ModelsView();
        private readonly GalleryView _galleryView = new GalleryView();

        public MainWindow()
        {
            InitializeComponent();

            // Load DashboardView by default
            MainContent.Content = _dashboardView;
            
            GalleryService.Img2ImgSelected += bmp =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    MainContent.Content = _dashboardView;
                    _dashboardView.SetInitImage(bmp);
                });
            };
            
        }

        private void MenuToggleButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle visibility
            if (SideMenu != null)
                SideMenu.IsVisible = !SideMenu.IsVisible;
        }

        private void ShowIntro_Click(object sender, RoutedEventArgs e)
        {
            var window = this.VisualRoot as Window;
            if (window == null) return;

            var IntWindow = new IntroWindow();
            IntWindow.Title = "Intro";
            IntWindow.ShowDialog(window);
        }

        private void ShowDashboard_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = _dashboardView;
            if (SideMenu != null)
                SideMenu.IsVisible = !SideMenu.IsVisible;
        }
        
        private void ShowSettings_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = _settingsView;
            if (SideMenu != null)
                SideMenu.IsVisible = !SideMenu.IsVisible;
        }
        
        private void ShowModels_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = _modelsView;
            if (SideMenu != null)
                SideMenu.IsVisible = !SideMenu.IsVisible;
        }
        
        private void ShowGallery_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = _galleryView;
            if (SideMenu != null)
                SideMenu.IsVisible = !SideMenu.IsVisible;
        }
    }
}
