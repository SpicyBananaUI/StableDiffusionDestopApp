using System;
using System.ComponentModel.Design;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using Avalonia.Media.Imaging;
using System.Collections.Generic;
using Avalonia.Controls;

namespace myApp.Services
{
    public class ApiService
    {
        // HttpClient is intended to be instantiated once per application, rather than per-use.
        private readonly HttpClient _httpClient = new();

        // GenerateImage method to call the Stable Diffusion API
        public async Task<(Bitmap?, long)> GenerateImage(string prompt, int steps, double guidanceScale, string negativePrompt = "", int width = 512, int height = 512, string sampler = "Euler", long seed = -1)
        {
            // Prompt for API Syntax
            var requestData = new
            {
                prompt = prompt,
                negative_prompt = negativePrompt,
                steps = steps,
                cfg_scale = guidanceScale,
                width = width,
                height = height,
                sampler_name = sampler,
                seed = seed
            };

            // Set up the HTTP request
            var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

            // Send the request to the API
            // Note: Ensure that the URL is correct and the API is running
            var response = await _httpClient.PostAsync("http://127.0.0.1:7861/sdapi/v1/txt2img", content);

            // Check if the response is successful
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"SD API returned error: {response.StatusCode}");
            }

            // Read the response content
            var jsonString = await response.Content.ReadAsStringAsync();

            // Parse the JSON response to extract the image data
            using var doc = JsonDocument.Parse(jsonString);
            var base64Image = doc.RootElement.GetProperty("images")[0].GetString();

            // Convert the base64 string to a Bitmap
            if (string.IsNullOrEmpty(base64Image))
                return (null, -1);

            // Decode the base64 string to a byte array and create a Bitmap
            byte[] imageBytes = Convert.FromBase64String(base64Image);
            using var ms = new MemoryStream(imageBytes);
            var bitmap = new Bitmap(ms);

            // Get the seed used
            var infoJson = doc.RootElement.GetProperty("info").GetString();
            using var infoDoc = JsonDocument.Parse(infoJson);
            long seedUsed = infoDoc.RootElement.GetProperty("seed").GetInt64();

            return (bitmap, seedUsed);
        }

        public class ProgressInfo
        {
            public float Progress { get; set; }
            public float EtaSeconds { get; set; }
        }
        
        public async Task<ProgressInfo> GetProgressAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://127.0.0.1:7861/sdapi/v1/progress");
                if (!response.IsSuccessStatusCode)
                    return new ProgressInfo();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                
                float progress = doc.RootElement.GetProperty("progress").GetSingle();
                float eta = doc.RootElement.TryGetProperty("eta_relative", out var etaProp) ? etaProp.GetSingle() : 0;
                
                return  new ProgressInfo() { Progress = progress, EtaSeconds = eta };
            }
            catch
            {
                return new  ProgressInfo();
            }
        }
        
        public async Task StopGenerationAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("http://127.0.0.1:7861/sdapi/v1/interrupt", null);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to stop generation: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while stopping generation: {ex.Message}");
            }
        }


        public async Task<List<string>> GetAvailableModelsAsync()
        {
            var response = await _httpClient.GetAsync("http://127.0.0.1:7861/sdapi/v1/sd-models");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var models = new List<string>();
            foreach (var model in doc.RootElement.EnumerateArray())
            {
                if (model.TryGetProperty("model_name", out var modelNameElement) && modelNameElement.ValueKind == JsonValueKind.String)
                {
                    var modelName = modelNameElement.GetString();
                    if (!string.IsNullOrEmpty(modelName))
                    {
                        models.Add(modelName);
                    }
                }
            }
            return models;
        }

        public async Task SetModelAsync(string modelName)
        {
            var content = new StringContent(
            JsonSerializer.Serialize(new { sd_model_checkpoint = modelName }),
            Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("http://127.0.0.1:7861/sdapi/v1/options", content);
            response.EnsureSuccessStatusCode();
        }
    
        public async Task<List<string>> GetAvailableSamplersAsync(){
            var response = await _httpClient.GetAsync("http://127.0.0.1:7861/sdapi/v1/samplers");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var samplerNames = new List<string>();

            foreach (var sampler in doc.RootElement.EnumerateArray())
            {
                if (sampler.TryGetProperty("name", out var nameProp) && nameProp.GetString() is string name)
                {
                    samplerNames.Add(name);
                }
            }

            return samplerNames;
        }
        
    }
}
