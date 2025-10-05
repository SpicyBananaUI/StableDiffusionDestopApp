using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using myApp.Services;
using System;
using System.Diagnostics;

namespace myApp;

public partial class ModelsView : UserControl
{
    public ModelsView()
    {
        Debug.WriteLine("ModelsView constructor called.");

        InitializeComponent();

        var downloadButton = this.FindControl<Button>("DownloadModelButton");
        if (downloadButton != null)
        {
            Debug.WriteLine("DownloadModelButton found and event attached.");
            downloadButton.Click += OnDownloadModelButtonClick;
        }
        else
        {
            Debug.WriteLine("DownloadModelButton not found.");
        }
    }

    private async void OnDownloadModelButtonClick(object? sender, RoutedEventArgs e)
    {
        Debug.WriteLine("Download Model button clicked.");

        var modelUrlTextBox = this.FindControl<TextBox>("ModelUrlTextBox");
        var checksumTextBox = this.FindControl<TextBox>("ChecksumTextBox");

        string modelUrl = modelUrlTextBox?.Text?.Trim() ?? string.Empty;
        string? checksum = checksumTextBox?.Text?.Trim();

        // Basic sanitization
        if (string.IsNullOrWhiteSpace(modelUrl) || !Uri.IsWellFormedUriString(modelUrl, UriKind.Absolute))
        {
            Debug.WriteLine("Invalid model URL.");
            return;
        }

        try
        {
            var apiService = new ApiService();
            string result = await apiService.DownloadModelAsync(modelUrl, checksum);
            Debug.WriteLine($"Model downloaded successfully: {result}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error downloading model: {ex.Message}");
        }
    }
}