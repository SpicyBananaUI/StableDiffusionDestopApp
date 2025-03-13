using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Avalonia.Media.Imaging;
using System.IO;

namespace myApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://localhost:8000";

        public ApiService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> GenerateImage(string prompt, int steps, double scale)
        {
            var url = $"{BaseUrl}/photo/{Uri.EscapeDataString(prompt)}/{steps}/{scale}/";
            
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error calling API: {ex.Message}");
                throw;
            }
        }

        public async Task<Bitmap?> LoadGeneratedImage()
        {
            try
            {
                // Download the image directly from the API
                var url = $"{BaseUrl}/image";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var imageStream = await response.Content.ReadAsStreamAsync();
                    return new Bitmap(imageStream);
                }
                else
                {
                    Console.WriteLine($"Failed to download image: {response.StatusCode}");
                    
                    // Fallback to the local file path as a backup
                    string filePath = "outputImages/generated_image.png";
                    if (File.Exists(filePath))
                    {
                        return new Bitmap(filePath);
                    }
                    
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image: {ex.Message}");
                return null;
            }
        }
    }
}
