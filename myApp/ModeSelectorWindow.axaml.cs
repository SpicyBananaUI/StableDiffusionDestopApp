using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace myApp;

public partial class ModeSelectorWindow : Window
{
    public ModeSelectorWindow()
    {
        InitializeComponent();
    }
    
    private void Local_Click(object? sender, RoutedEventArgs e)
    {
        Close(App.RunMode.Local);
    }

    private void Server_Click(object? sender, RoutedEventArgs e)
    {
        Close(App.RunMode.RemoteServer);
    }

    private async void Client_Click(object? sender, RoutedEventArgs e)
    {
        // Ask user for server address (popup input dialog)
        var input = new RemoteAddressDialog();
        var result = await input.ShowDialog<string>(this);
        if (!string.IsNullOrEmpty(result))
        {
            App.AppConfig.RemoteAddress = result.TrimEnd('/');
            Close(App.RunMode.RemoteClient);
        }
    }

}