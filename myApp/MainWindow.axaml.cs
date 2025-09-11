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
using System.Collections.Generic;

namespace myApp;

public partial class MainWindow : Window
{
    private readonly DashboardView _dashboardView = new DashboardView();
    private readonly SettingsView _settingsView = new SettingsView();
    private readonly ModelsView _modelsView = new ModelsView();
    
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
            modelsButton.Click += (_, _) => MainContent.Content = _modelsView;

    }
    
}