using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using myApp.Services;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using myApp.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace myApp;

public partial class GalleryView : UserControl
{
    public GalleryView()
    {
        InitializeComponent();
    }
    

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Bitmap bitmap)
        {
            var dialog = new FilePickerSaveOptions
            {
                SuggestedFileName = "generated.png",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PNG Image") { Patterns = new[] { "*.png" } }
                }
            };

            var window = this.VisualRoot as Window;
            var file = await window!.StorageProvider.SaveFilePickerAsync(dialog);

            if (file != null)
            {
                try
                {
                    await using var stream = await file.OpenWriteAsync();
                    bitmap.Save(stream);
                }
                catch (Exception ex)
                {
                    var msgBox = new Window
                    {
                        Content = new TextBlock { Text = $"Failed to save image: {ex.Message}" },
                        Width = 300,
                        Height = 100
                    };
                    await msgBox.ShowDialog(window);
                }
            }
        }
    }

    private void OnUseClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Bitmap bitmap)
        {
            GalleryService.SetImageForImg2Img(bitmap);
        }
    }
    
}