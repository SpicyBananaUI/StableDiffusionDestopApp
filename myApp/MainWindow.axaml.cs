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

namespace myApp;

public partial class MainWindow : Window
{
    private readonly DashboardView _dashboardView = new DashboardView();
    private readonly SettingsView _settingsView = new SettingsView();
    private readonly ModelsView _modelsView = new ModelsView();
    private readonly GalleryView _galleryView = new GalleryView();
    
    public MainWindow()
    {
        InitializeComponent();
        this.WindowState = WindowState.Maximized;

        MainContent.Content = _dashboardView;
        
        if (this.FindControl<Button>("DashboardButton") is Button dashboardButton)
            dashboardButton.Click += (_, _) => MainContent.Content = _dashboardView;

        if (this.FindControl<Button>("SettingsButton") is Button settingsButton)
            settingsButton.Click += (_, _) => MainContent.Content = _settingsView;
        
        if (this.FindControl<Button>("ModelsButton") is Button modelsButton)
            modelsButton.Click += (_, _) =>     {
                Debug.WriteLine("ModelsButton clicked.");
                MainContent.Content = _modelsView;
            };
        
        if (this.FindControl<Button>("GalleryButton") is Button galleryButton)
            galleryButton.Click += (_, _) => MainContent.Content = _galleryView;
        
        GalleryService.Img2ImgSelected += bmp =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                MainContent.Content = _dashboardView;
                _dashboardView.SetInitImage(bmp);
            });
        };
    }
    
    
    private void ShowIntro(object sender, RoutedEventArgs e)
    {
        var window = this.VisualRoot as Window;
        if (window == null) return;

        var IntWindow = new IntroWindow();
        IntWindow.Title = "Intro";
        IntWindow.ShowDialog(window);
    }
    
}