using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using System.IO;
using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;

namespace myApp.Services;

public static class GalleryService
{
    public static ObservableCollection<Bitmap> RecentImages { get; } = new();
    private const int MaxImages = 25;

    public static Bitmap? Img2ImgSource { get; private set; }

    public static event Action<Bitmap>? Img2ImgSelected;

    public static void AddRecentImage(Bitmap image)
    {
        if (image == null) return;

        RecentImages.Insert(0, image);
        if (RecentImages.Count > MaxImages)
            RecentImages.RemoveAt(RecentImages.Count - 1);
    }
    
    public static void SetImageForImg2Img(Bitmap bmp)
    {
        Img2ImgSource = bmp;
        Img2ImgSelected?.Invoke(bmp);
    }

}