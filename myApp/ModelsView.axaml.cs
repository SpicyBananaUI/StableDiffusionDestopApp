using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using myApp.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace myApp;

public partial class ModelsView : UserControl
{
    private record ModelEntry(string name, string url, string? checksum, string? thumbnail);

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

        // Load models.json and render cards
        try
        {
            var models = LoadModelsFromJson();
            RenderModelCards(models);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load models.json: {ex.Message}");
        }
    }

    private async void OnDownloadModelButtonClick(object? sender, RoutedEventArgs e)
    {
        Debug.WriteLine("Download Model button clicked.");

        var modelUrlTextBox = this.FindControl<TextBox>("ModelUrlTextBox");
        var checksumTextBox = this.FindControl<TextBox>("ChecksumTextBox");
        var progressBar = this.FindControl<ProgressBar>("ModelDownloadProgress");
        var statusText = this.FindControl<TextBlock>("ModelDownloadStatus");

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
            if (statusText != null) statusText.Text = "Starting download...";
            if (progressBar != null)
            {
                progressBar.IsVisible = true;
                progressBar.IsIndeterminate = true;
            }

            string downloadId;
            try
            {
                downloadId = await apiService.StartDownloadModelAsync(modelUrl, checksum);
            }
            catch (InvalidOperationException)
            {
                if (statusText != null) statusText.Text = "Model already exists on server";
                return;
            }
            if (statusText != null) statusText.Text = "Downloading model...";

            // Poll progress periodically without blocking UI
            if (progressBar != null) progressBar.IsIndeterminate = false;
            var sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                await Task.Delay(1000);
                var prog = await apiService.GetDownloadModelProgressAsync(downloadId);

                // Update UI
                if (progressBar != null)
                {
                    if (prog.TotalBytes > 0)
                    {
                        progressBar.Minimum = 0;
                        progressBar.Maximum = prog.TotalBytes;
                        progressBar.Value = prog.DownloadedBytes;
                    }
                    else
                    {
                        progressBar.IsIndeterminate = true;
                    }
                }
                if (statusText != null)
                {
                    string pct = prog.TotalBytes > 0 ? $" {(int)(prog.Progress * 100)}%" : "";
                    statusText.Text = prog.Status == "in_progress" ? $"Downloading{pct}" : prog.Status;
                }

                if (prog.Status == "completed")
                {
                    if (statusText != null) statusText.Text = "Download complete";
                    // Ask dashboard to reload models
                    if (DashboardView._instance != null)
                    {
                        await DashboardView._instance.ReloadModelsAsync();
                    }
                    break;
                }
                if (prog.Status == "failed")
                {
                    if (statusText != null) statusText.Text = $"Download failed: {prog.Error}";
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error downloading model: {ex.Message}");
            if (statusText != null) statusText.Text = "Download failed";
        }
        finally
        {
            if (progressBar != null) progressBar.IsIndeterminate = false;
        }
    }

    private List<ModelEntry> LoadModelsFromJson()
    {
        // Try to locate models.json next to the executable
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string candidate1 = Path.Combine(baseDir, "models.json");
        string candidate2 = Path.Combine(baseDir, "../../../myApp", "models.json");

        string jsonPath = File.Exists(candidate1) ? candidate1 : candidate2;
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException($"models.json not found at '{candidate1}' or '{candidate2}'");

        string json = File.ReadAllText(jsonPath);
        var models = JsonSerializer.Deserialize<List<ModelEntry>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return models ?? new List<ModelEntry>();
    }

    private void RenderModelCards(List<ModelEntry> models)
    {
        var wrap = this.FindControl<WrapPanel>("ModelsWrapPanel");
        var modelUrlTextBox = this.FindControl<TextBox>("ModelUrlTextBox");
        var checksumTextBox = this.FindControl<TextBox>("ChecksumTextBox");
        if (wrap == null) return;

        wrap.Children.Clear();

        foreach (var m in models)
        {
            var border = new Border
            {
                Width = 200,
                Height = 280,
                Background = Avalonia.Media.Brushes.DimGray,
                CornerRadius = new Avalonia.CornerRadius(10),
                Padding = new Thickness(8)
            };

            var stack = new StackPanel();

            var image = new Image
            {
                Source = null,
                Stretch = Avalonia.Media.Stretch.UniformToFill,
                Height = 150,
                Margin = new Thickness(0,0,0,5)
            };

            // Optional: thumbnail not wired to resources; leaving blank unless future asset binding provided
            var title = new TextBlock { Text = m.name, FontWeight = Avalonia.Media.FontWeight.Bold, Margin = new Thickness(0,0,0,5) };
            var subtitle = new TextBlock { Text = m.url, FontSize = 12 };
            var progress = new ProgressBar { Minimum = 0, Maximum = 100, Value = 0, Height = 10, Margin = new Thickness(0,5,0,5) };
            var button = new Button { Content = "Download", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };

            button.Click += (_, __) =>
            {
                if (modelUrlTextBox != null) modelUrlTextBox.Text = m.url;
                if (checksumTextBox != null) checksumTextBox.Text = m.checksum ?? string.Empty;
            };

            stack.Children.Add(image);
            stack.Children.Add(title);
            stack.Children.Add(subtitle);
            stack.Children.Add(progress);
            stack.Children.Add(button);
            border.Child = stack;
            wrap.Children.Add(border);
        }
    }
}