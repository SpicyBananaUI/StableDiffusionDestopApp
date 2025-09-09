using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;

namespace myApp
{
    public partial class SlideshowWindow : Window
    {
        private int currentSlide = 0;
        private Grid[] slides;
        private Button prevButton;
        private Button nextButton;

        public SlideshowWindow()
        {
            AvaloniaXamlLoader.Load(this);
            
#if DEBUG
            this.AttachDevTools();
#endif
            
            // Subscribe to the Loaded event instead of using OnInitialized
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Now all controls are fully loaded and accessible
            try
            {
                slides = new Grid[] { 
                    this.FindControl<Grid>("Slide1"),
                    this.FindControl<Grid>("Slide2"), 
                    this.FindControl<Grid>("Slide3"),
                    this.FindControl<Grid>("Slide4") 
                };
                
                prevButton = this.FindControl<Button>("PrevButton");
                nextButton = this.FindControl<Button>("NextButton");
                
                // Set initial button states
                prevButton.IsEnabled = false;
                
                // Ensure only the first slide is visible initially
                if (slides.Length > 0)
                {
                    for (int i = 1; i < slides.Length; i++)
                    {
                        slides[i].IsVisible = false;
                    }
                    slides[0].IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing slides: {ex.Message}");
                // Fallback: close the window or show an error
                Close();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentSlide > 0 && slides != null)
            {
                slides[currentSlide].IsVisible = false;
                currentSlide--;
                slides[currentSlide].IsVisible = true;
                
                nextButton.IsEnabled = true;
                if (currentSlide == 0)
                    prevButton.IsEnabled = false;
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (slides != null && currentSlide < slides.Length - 1)
            {
                slides[currentSlide].IsVisible = false;
                currentSlide++;
                slides[currentSlide].IsVisible = true;
                
                prevButton.IsEnabled = true;
                if (currentSlide == slides.Length - 1)
                    nextButton.IsEnabled = false;
            }
        }
    }
}