using Avalonia.Controls;
using Avalonia.Interactivity;
using myApp.Services;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia;
using System;

namespace myApp;

public partial class MainWindow : Window
{
    private readonly ApiService _apiService;
    
    public MainWindow()
    {
        InitializeComponent();
        
        _apiService = new ApiService();
        
        // Set up event handlers
        if (this.FindControl<Button>("GenerateButton") is Button generateButton)
            generateButton.Click += OnGenerateButtonClick;
        
        if (this.FindControl<Slider>("StepsSlider") is Slider stepsSlider)
            stepsSlider.PropertyChanged += (s, e) => {
                if (e.Property == Slider.ValueProperty && this.FindControl<TextBlock>("StepsValueText") is TextBlock stepsText)
                    stepsText.Text = $"{Math.Round(stepsSlider.Value)}";
            };
        
        if (this.FindControl<Slider>("ScaleSlider") is Slider scaleSlider)
            scaleSlider.PropertyChanged += (s, e) => {
                if (e.Property == Slider.ValueProperty && this.FindControl<TextBlock>("ScaleValueText") is TextBlock scaleText)
                    scaleText.Text = $"{scaleSlider.Value:F1}";
            };
    }

    private async void OnGenerateButtonClick(object sender, RoutedEventArgs e)
    {
        var promptTextBox = this.FindControl<TextBox>("PromptTextBox");
        var stepsSlider = this.FindControl<Slider>("StepsSlider");
        var scaleSlider = this.FindControl<Slider>("ScaleSlider");
        var resultImage = this.FindControl<Image>("ResultImage");
        var statusText = this.FindControl<TextBlock>("StatusText");
        var loadingBar = this.FindControl<ProgressBar>("LoadingBar");
        var generateButton = this.FindControl<Button>("GenerateButton");

        if (string.IsNullOrWhiteSpace(promptTextBox?.Text))
        {
            statusText.Text = "Please enter a prompt";
            return;
        }

        try
        {
            // Update UI state
            generateButton.IsEnabled = false;
            resultImage.Source = null;
            statusText.Text = "Generating image...";
            loadingBar.IsVisible = true;
            
            // Call API
            string prompt = promptTextBox.Text;
            int steps = (int)Math.Round(stepsSlider.Value);
            double scale = scaleSlider.Value;
            
            await _apiService.GenerateImage(prompt, steps, scale);
            
            // Load the generated image
            var bitmap = await _apiService.LoadGeneratedImage();
            if (bitmap != null)
            {
                resultImage.Source = bitmap;
                statusText.Text = string.Empty;
            }
            else
            {
                statusText.Text = "Failed to load generated image";
            }
        }
        catch (Exception ex)
        {
            statusText.Text = $"Error: {ex.Message}";
        }
        finally
        {
            loadingBar.IsVisible = false;
            generateButton.IsEnabled = true;
        }
    }
}