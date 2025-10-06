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
using System.Linq;

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

        // Initialize Installed Models tab
        _ = InitializeInstalledModelsTabAsync();

        // Initialize Built-in Plugins UI
        _ = InitializeBuiltinPluginsUI();
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

    private List<string> _backendSafeSelected = new();
    private List<string> _unsafeSelected = new();

    private async Task InitializeBuiltinPluginsUI()
    {
        try
        {
            var api = new ApiService();
            var extensions = await api.GetExtensionsAsync();

            // Separate backend-safe vs unsafe by simple heuristic (same as backend): no javascript folder and no gradio imports known server-side.
            // The backend-safe set is what /enable-backend-safe would choose; here we just display based on names in list that backend exposes.
            var backendSafe = new List<ApiService.ExtensionInfo>();
            var unsafeList = new List<ApiService.ExtensionInfo>();

            // We cannot locally check file contents; ask backend to compute and return the set by calling the endpoint with dry run behavior.
            // Fallback: classify by known prefixes likely safe.
            foreach (var e in extensions)
            {
                if (e.name.StartsWith("forge_preprocessor_") || e.name == "SwinIR" || e.name == "ScuNET" || e.name == "soft-inpainting")
                    backendSafe.Add(e);
                else
                    unsafeList.Add(e);
            }

            var panelSafe = this.FindControl<StackPanel>("BackendSafeExtensionsPanel");
            var panelUnsafe = this.FindControl<StackPanel>("UnsafeExtensionsPanel");
            var btnToggleSelect = this.FindControl<Button>("ToggleSelectBackendSafeButton");
            var btnApplySelected = this.FindControl<Button>("ApplySelectedBackendSafeButton");
            var btnToggleUnsafe = this.FindControl<Button>("DeselectUnsafeButton");
            var btnApplyUnsafe = this.FindControl<Button>("ApplySelectedUnsafeButton");

            if (panelSafe != null)
            {
                panelSafe.Children.Clear();
                foreach (var e in backendSafe.OrderBy(x => x.name))
                {
                    var cb = new CheckBox { Content = e.name, IsChecked = e.enabled };
                    cb.Checked += (_, __) => { if (!_backendSafeSelected.Contains(e.name)) _backendSafeSelected.Add(e.name); };
                    cb.Unchecked += (_, __) => { _backendSafeSelected.RemoveAll(x => x == e.name); };
                    panelSafe.Children.Add(cb);
                    if (e.enabled && !_backendSafeSelected.Contains(e.name)) _backendSafeSelected.Add(e.name);
                }
            }

            if (panelUnsafe != null)
            {
                panelUnsafe.Children.Clear();
                foreach (var e in unsafeList.OrderBy(x => x.name))
                {
                    var cb = new CheckBox { Content = e.name, IsChecked = e.enabled };
                    cb.Checked += (_, __) =>
                    {
                        if (!_unsafeSelected.Contains(e.name))
                            _unsafeSelected.Add(e.name);
                    };
                    cb.Unchecked += (_, __) =>
                    {
                        _unsafeSelected.RemoveAll(x => x == e.name);
                    };
                    panelUnsafe.Children.Add(cb);
                    if (e.enabled && !_unsafeSelected.Contains(e.name))
                        _unsafeSelected.Add(e.name);
                }
            }

            // BACKEND-SAFE extension buttons
            if (btnToggleSelect != null && panelSafe != null)
            {
                btnToggleSelect.Click += (_, __) =>
                {
                    // If not all selected, select all; else deselect all
                    var checkboxes = panelSafe.Children.OfType<CheckBox>().ToList();
                    bool allSelected = checkboxes.All(c => c.IsChecked == true);
                    bool target = !allSelected;
                    foreach (var cb in checkboxes)
                    {
                        cb.IsChecked = target;
                    }
                    btnToggleSelect.Content = target ? "Deselect All" : "Select All";
                };
            }

            if (btnApplySelected != null)
            {
                btnApplySelected.Click += async (_, __) =>
                {
                    try
                    {
                        // Merge selected backend-safe with any other currently enabled non-safe extensions to avoid accidentally disabling them
                        var current = await api.GetExtensionsAsync();
                        var otherEnabled = current.Where(x => x.enabled && !_backendSafeSelected.Contains(x.name)).Select(x => x.name);
                        var target = otherEnabled.Concat(_backendSafeSelected).Distinct().ToList();
                        var ok = await api.EnableExtensionsAsync(target);
                        Debug.WriteLine(ok ? "Enabled selected backend-safe extensions" : "Enable selected backend-safe failed");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Enable selected backend-safe error: {ex.Message}");
                    }
                };
            }

            // UNSAFE extension buttons
            if (btnToggleUnsafe != null && panelUnsafe != null)
            {
                // Always acts as "Deselect All"
                btnToggleUnsafe.Click += (_, __) =>
                {
                    var checkboxes = panelUnsafe.Children.OfType<CheckBox>().ToList();
                    foreach (var cb in checkboxes)
                        cb.IsChecked = false;

                    btnToggleUnsafe.Content = "Deselect All"; // Always stays the same
                };
            }

            if (btnApplyUnsafe != null)
            {
                btnApplyUnsafe.Click += async (_, __) =>
                {
                    try
                    {
                        var current = await api.GetExtensionsAsync();
                        var otherEnabled = current
                            .Where(x => x.enabled && !_unsafeSelected.Contains(x.name))
                            .Select(x => x.name);
                        var target = otherEnabled.Concat(_unsafeSelected).Distinct().ToList();

                        var ok = await api.EnableExtensionsAsync(target);
                        Debug.WriteLine(ok ? "Enabled selected unsafe extensions" : "Enable selected unsafe failed");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Enable selected unsafe error: {ex.Message}");
                    }
                };
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize Built-in Plugins UI: {ex.Message}");
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

            // TODO: update to also do release path if release build
            var stack = new StackPanel();
            var img_path =  Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Assets", m.thumbnail);


            var image = new Image
            {
                Source = m.thumbnail != null && File.Exists(img_path) ?
                    new Avalonia.Media.Imaging.Bitmap(img_path) :
                    null,
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

    private async Task InitializeInstalledModelsTabAsync()
    {
        var api = new ApiService();
        var listBox = this.FindControl<ListBox>("InstalledModelsList");
        var refreshButton = this.FindControl<Button>("RefreshModelsButton");

        if (listBox == null)
        {
            Debug.WriteLine("InstalledModelsList not found in XAML!");
            return;
        }

        async Task LoadModelsAsync()
        {
            await api.RefreshCheckpointsAsync();
            var models = await api.GetAvailableModelsAsync();
            Debug.WriteLine($"[ModelsView] Loaded {models?.Count ?? 0} models: {string.Join(", ", models ?? new List<string>())}");

            listBox.Items.Clear();

            if (models == null || models.Count == 0)
            {
                listBox.Items.Add(new TextBlock { Text = "No models found", Margin = new Thickness(5) });
                return;
            }

            foreach (var modelName in models.OrderBy(x => x))
            {
                var row = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Margin = new Thickness(5),
                    Spacing = 10
                };

                var nameText = new TextBlock
                {
                    Text = modelName,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                var selectButton = new Button { Content = "Select", Width = 80 };
                selectButton.Click += async (_, __) =>
                {
                    try
                    {
                        await api.SetModelAsync(modelName);
                        Debug.WriteLine($"Switched to model: {modelName}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to select model {modelName}: {ex.Message}");
                    }
                };

                var deleteButton = new Button { Content = "Delete", Width = 80 };
                deleteButton.Click += async (_, __) =>
                {
                    try
                    {
                        var ok = await api.DeleteModelAsync(modelName);
                        if (ok)
                        {
                            Debug.WriteLine($"Deleted model: {modelName}");
                            await LoadModelsAsync(); // refresh list
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error deleting model {modelName}: {ex.Message}");
                    }
                };

                row.Children.Add(nameText);
                row.Children.Add(selectButton);
                row.Children.Add(deleteButton);
                listBox.Items.Add(row);
            }
        }

        if (refreshButton != null)
            refreshButton.Click += async (_, __) => await LoadModelsAsync();

        await LoadModelsAsync();
    }


}