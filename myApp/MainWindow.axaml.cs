// Copyright (c) 2025 Spicy Banana
// SPDX-License-Identifier: AGPL-3.0-only

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
    private readonly ApiService _apiService;
    private List<Bitmap> _generatedImages = new List<Bitmap>(); // Store all generated images
    private int _currentImageIndex = 0; // Index of the currently displayed image

    private List<long> _seeds = new List<long>(); // Store the seeds for each generated image
    
    private bool _isGenerating = false; // Keep track of current state of generation
    private CancellationTokenSource? _generationCts;
    
    public MainWindow()
    {
        InitializeComponent();
        this.WindowState = WindowState.Maximized;
        
        // Initialize the API service
        _apiService = new ApiService();

        // Set up default model and watch for changes
        InitUIAsync();
        
        
        // Set up event handlers
        if (this.FindControl<Button>("GenerateButton") is Button generateButton)
        {
            generateButton.Click += OnGenerateButtonClick;
            generateButton.Content = "Generate";
        }

        if (this.FindControl<Button>("SaveButton") is Button saveButton)
            saveButton.Click += OnSaveButtonClick;

        
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

        if (this.FindControl<Slider>("BatchSlider") is Slider batchSlider)
        {
            batchSlider.PropertyChanged += (s, e) => {
            if (e.Property == Slider.ValueProperty && this.FindControl<TextBlock>("BatchValueText") is TextBlock batchText)
                batchText.Text = $"{Math.Round(batchSlider.Value)}";
            };
        }

        this.FindControl<Button>("PrevImageButton").Click += (s, e) => ShowImageAt(_currentImageIndex - 1);
        this.FindControl<Button>("NextImageButton").Click += (s, e) => ShowImageAt(_currentImageIndex + 1);

    }

    private async void OnGenerateButtonClick(object sender, RoutedEventArgs e)
    {
        var generateButton = this.FindControl<Button>("GenerateButton");

        if (_isGenerating)
        {
            // Since a generation was ongoing, this press must have been a cancel
            generateButton.Content = "Cancelling Generation...";
            generateButton.IsEnabled = false;           // Will be re-enabled by the generate call finally
            _generationCts?.Cancel();
            await _apiService.StopGenerationAsync();  // Interrupt api call
            return;
        }

        _isGenerating = true;
        // Cancellation Token Source for generation progress bar
        _generationCts = new CancellationTokenSource();

        
        // Get controls
        var promptTextBox = this.FindControl<TextBox>("PromptTextBox");
        var negativePromptTextBox = this.FindControl<TextBox>("NegativePromptTextBox");
        var stepsSlider = this.FindControl<Slider>("StepsSlider");
        var scaleSlider = this.FindControl<Slider>("ScaleSlider");
        var batchSlider = this.FindControl<Slider>("BatchSlider");
        var widthTextBox = this.FindControl<TextBox>("WidthTextBox");
        var heightTextBox = this.FindControl<TextBox>("HeightTextBox");
        var resultImage = this.FindControl<Image>("ResultImage");
        var statusText = this.FindControl<TextBlock>("StatusText");
        var loadingBar = this.FindControl<ProgressBar>("LoadingBar");
        var progressText = this.FindControl<TextBlock>("ProgressText");
        var saveButton = this.FindControl<Button>("SaveButton");
        var samplerComboBox = this.FindControl<ComboBox>("SamplerComboBox");
        var modelComboBox =  this.FindControl<ComboBox>("ModelComboBox");
        var seedTextBox = this.FindControl<TextBox>("SeedTextBox");


        // Validate input
        if (string.IsNullOrWhiteSpace(promptTextBox?.Text))
        {
            statusText.Text = "Please enter a prompt";
            return;
        }

        try
        {
            // Update UI state
            _isGenerating = true;
            generateButton.Content = "Cancel";
            SaveButton.IsEnabled = false;
            resultImage.Source = null;
            statusText.Text = "Generating image...";
            
            
            // Call API
            string prompt = promptTextBox.Text;
            string negativePrompt = negativePromptTextBox?.Text ?? string.Empty;
            int steps = (int)Math.Round(stepsSlider.Value);
            double scale = scaleSlider.Value;
            int batchSize = (int)Math.Round(batchSlider.Value);
            int width = int.TryParse(widthTextBox?.Text, out var w) ? Math.Max(64,Math.Min(w,2048)) : 512;
            int height = int.TryParse(heightTextBox?.Text, out var h) ? Math.Max(64,Math.Min(h,2048)) : 512;
            string sampler = samplerComboBox?.SelectedItem as string ?? string.Empty;
            long seed = long.TryParse(seedTextBox?.Text, out var s) ? s : -1;

            // Get History
            this.FindControl<TextBlock>("InfoPromptText").Text = $"Prompt: {prompt}";
            this.FindControl<TextBlock>("InfoNegativePromptText").Text = $"Negative Prompt: {negativePrompt}";
            this.FindControl<TextBlock>("InfoStepsText").Text = $"Steps: {steps}, CFG: {scale}";
            this.FindControl<TextBlock>("InfoSamplerText").Text = $"Sampler: {sampler}, Model: {modelComboBox?.SelectedItem}";
            this.FindControl<TextBlock>("InfoSizeText").Text = $"Size: {width} x {height}";

            bool done = false;

            var pollingTask = Task.Run(async () =>
            {
                try
                {

                    while (!_generationCts.Token.IsCancellationRequested && !done && _isGenerating)
                    {
                        var progressInfo = await _apiService.GetProgressAsync();

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            loadingBar.Value = progressInfo.Progress * 100;
                        
                            string etaText = progressInfo.EtaSeconds > 0 ?  $" (ETA: {progressInfo.EtaSeconds:F1})s" : string.Empty;
                        
                            progressText.Text = $"Generating... {Math.Round(progressInfo.Progress * 100)}%{etaText}";

                            ProgressOverlay.IsVisible = true;
                            ResultImage.IsVisible = false;
                        });

                        // if (ct)//progressInfo.Progress >= 1.0f)
                        // {
                        //     done = true;
                        //     break;
                        // }
                    
                        await Task.Delay(500, _generationCts.Token);
                    }
                }
                catch (OperationCanceledException){
                    //Expected
                }
            });
            
            var (images, seeds) = await _apiService.GenerateImage(prompt, steps, scale, negativePrompt, width, height, sampler, seed, batchSize);

            _generatedImages = images; // Store all generated images
            _seeds = seeds; // Store all generated seeds
            _currentImageIndex = 0; // Reset the current image index
            ShowImageAt(_currentImageIndex); // Show the first generated image

            this.FindControl<StackPanel>("ImageNavPanel").IsVisible = _generatedImages.Count > 1; // Show navigation panel if multiple images are generated

            _isGenerating = false;
            generateButton.Content = "Generate";

            // Record the seed in history
            this.FindControl<TextBlock>("InfoSeedText").Text = $"Seed: {seeds[_currentImageIndex]}";
            
            _generationCts?.Cancel();
            await pollingTask;
            
            // Load the generated image
            if (images[0] != null)
            {
                resultImage.Source = images[0];
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
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                loadingBar.Value = 0;
                progressText.Text = "Done!";
                generateButton.IsEnabled = true;
                ResultImage.IsVisible = true;
                ProgressOverlay.IsVisible = false;
                _isGenerating = false;
                saveButton.IsEnabled = true;
                generateButton.Content = "Generate";
            });
        }
    }

    private async void OnSaveButtonClick(object? sender, RoutedEventArgs e){

        if (_generatedImages[_currentImageIndex] == null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Save Generated Image",
            InitialFileName = "generated.png",
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter
                {
                    Name = "PNG Image",
                    Extensions = { "png" }
                }
            }
        };

        string? filePath = await dialog.ShowAsync(this);
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            try
            {
                using var stream = File.Create(filePath);
                _generatedImages[_currentImageIndex].Save(stream);
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusText.Text = $"Failed to save image: {ex.Message}";
                });
            }
        }


    }

    private async void InitUIAsync()
    {
        try
        {
            // Initialize UI components here if needed
            var modelComboBox = this.FindControl<ComboBox>("ModelComboBox");
            var models = await _apiService.GetAvailableModelsAsync();

            var samplerComboBox = this.FindControl<ComboBox>("SamplerComboBox");
            var samplers = await _apiService.GetAvailableSamplersAsync();
        
            if (modelComboBox != null)
            {
                modelComboBox.ItemsSource = models;
                modelComboBox.SelectedIndex = 0; // Select the first model by default
            }

            if (samplerComboBox != null)
            {
                samplerComboBox.ItemsSource = samplers;
                samplerComboBox.SelectedValue = "Euler"; // Select the Euler sampler by default
            }

            modelComboBox.SelectionChanged += async (s, e) =>
            {
                if (modelComboBox.SelectedItem is string selectedModel)
                {
                    // Handle model selection change if needed
                    await _apiService.SetModelAsync(selectedModel);
                }
            };

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load models: {ex.Message}");
        }
    
    }

    private void ShowImageAt(int index)
    {
        if (_generatedImages.Count == 0 || index < 0 || index >= _generatedImages.Count){
            return;
        }
        
        _currentImageIndex = index;
        ResultImage.Source = _generatedImages[index];
        this.FindControl<TextBlock>("ImageIndexLabel").Text = $"Image {index + 1} of {_generatedImages.Count}";
        this.FindControl<TextBlock>("InfoSeedText").Text = $"Seed: {_seeds[_currentImageIndex]}";
    }
}