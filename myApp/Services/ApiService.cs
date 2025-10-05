// Copyright (c) 2025 Spicy Banana
// SPDX-License-Identifier: AGPL-3.0-only

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
using System.Diagnostics;

namespace myApp.Services
{
    public class ApiService
    {
        // HttpClient is intended to be instantiated once per application, rather than per-use.
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(App.AppConfig.RemoteAddress)
            };
        }

        // GenerateImage method to call the Stable Diffusion API
        public async Task<(List<Bitmap>, List<long>)> GenerateImage(string prompt, int steps, double guidanceScale, string negativePrompt = "", int width = 512, int height = 512, string sampler = "Euler", long seed = -1, int batch_size = 1)
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
                seed = seed,
                batch_size = batch_size,
            };

            // Set up the HTTP request
            var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

            // Send the request to the API
            // Note: Ensure that the URL is correct and the API is running
            //var response = await _httpClient.PostAsync("http://127.0.0.1:7861/sdapi/v1/txt2img", content);
            var response = await _httpClient.PostAsync("/sdapi/v1/txt2img", content);

            // Check if the response is successful
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"SD API returned error: {response.StatusCode}");
            }

            // Read the response content
            var jsonString = await response.Content.ReadAsStringAsync();

            // Parse the JSON response to extract the image data
            using var doc = JsonDocument.Parse(jsonString);
            var images = new List<Bitmap>();
            foreach (var imgBase64 in doc.RootElement.GetProperty("images").EnumerateArray())
            {
                var base64Image = imgBase64.GetString();
                if (string.IsNullOrEmpty(base64Image))
                    return (new List<Bitmap>(), new List<long>());

                // Decode the base64 string to a byte array and create a Bitmap
                byte[] imageBytes = Convert.FromBase64String(base64Image);
                using var ms = new MemoryStream(imageBytes);
                var bitmap = new Bitmap(ms);
                images.Add(bitmap);
            }
            //var base64Image = doc.RootElement.GetProperty("images")[0].GetString();

            // Convert the base64 string to a Bitmap
            //if (string.IsNullOrEmpty(base64Image))
                //return (null, -1);

            // Decode the base64 string to a byte array and create a Bitmap
            //byte[] imageBytes = Convert.FromBase64String(base64Image);
            //using var ms = new MemoryStream(imageBytes);
            //var bitmap = new Bitmap(ms);

            // Get the seed used
            var infoJson = doc.RootElement.GetProperty("info").GetString();
            using var infoDoc = JsonDocument.Parse(infoJson!);
            var seeds = infoDoc.RootElement.GetProperty("all_seeds").EnumerateArray();
            var seedList = new List<long>();
            foreach (var seedElement in seeds)
            {
                if (seedElement.TryGetInt64(out var seedValue))
                {
                    seedList.Add(seedValue);
                }
            }
            //long seedUsed = infoDoc.RootElement.GetProperty("seed").GetInt64();

            //return (bitmap, seedUsed);
            return (images, seedList);
        }
        
        public async Task<(List<Bitmap>, List<long>)> GenerateImage2Image(
            string prompt,
            int steps,
            double guidanceScale,
            Bitmap initImage,
            string negativePrompt = "",
            int width = 512,
            int height = 512,
            string sampler = "Euler",
            long seed = -1,
            int batch_size = 1,
            double denoisingStrength = 0.75)
        {
            if (initImage == null)
                throw new ArgumentNullException(nameof(initImage));

            // Convert Bitmap to Base64
            using var ms = new MemoryStream();
            initImage.Save(ms);
            string base64InitImage = Convert.ToBase64String(ms.ToArray());


            // Build request
            var requestData = new
            {
                prompt = prompt,
                negative_prompt = negativePrompt,
                steps = steps,
                cfg_scale = guidanceScale,
                width = width,
                height = height,
                sampler_name = sampler,
                seed = seed,
                batch_size = batch_size,
                denoising_strength = denoisingStrength,
                init_images = new[] { base64InitImage }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

            // POST to img2img endpoint
            //var response = await _httpClient.PostAsync("http://127.0.0.1:7861/sdapi/v1/img2img", content);
            var response = await _httpClient.PostAsync("/sdapi/v1/img2img", content);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"SD API returned error: {response.StatusCode}");

            var jsonString = await response.Content.ReadAsStringAsync();

            // Parse response
            using var doc = JsonDocument.Parse(jsonString);
            var images = new List<Bitmap>();

            foreach (var imgBase64 in doc.RootElement.GetProperty("images").EnumerateArray())
            {
                var base64Image = imgBase64.GetString();
                if (string.IsNullOrEmpty(base64Image))
                    return (new List<Bitmap>(), new List<long>());

                byte[] outBytes = Convert.FromBase64String(base64Image);
                using var ims = new MemoryStream(outBytes);
                var bitmap = new Bitmap(ims);
                images.Add(bitmap);
            }

            // Extract seeds
            var infoJson = doc.RootElement.GetProperty("info").GetString();
            using var infoDoc = JsonDocument.Parse(infoJson!);

            var seedList = new List<long>();
            foreach (var seedElement in infoDoc.RootElement.GetProperty("all_seeds").EnumerateArray())
            {
                if (seedElement.TryGetInt64(out var seedValue))
                    seedList.Add(seedValue);
            }

            return (images, seedList);
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
                //var response = await _httpClient.GetAsync("http://127.0.0.1:7861/sdapi/v1/progress");
                var response = await _httpClient.GetAsync("/sdapi/v1/progress");
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
                //var response = await _httpClient.PostAsync("http://127.0.0.1:7861/sdapi/v1/interrupt", null);
                var response = await _httpClient.PostAsync("/sdapi/v1/interrupt", null);
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
            //var response = await _httpClient.GetAsync("http://127.0.0.1:7861/sdapi/v1/sd-models");
            var response = await _httpClient.GetAsync("/sdapi/v1/sd-models");
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

            //var response = await _httpClient.PostAsync("http://127.0.0.1:7861/sdapi/v1/options", content);
            var response = await _httpClient.PostAsync("/sdapi/v1/options", content);
            response.EnsureSuccessStatusCode();
        }
        
        public async Task RefreshCheckpointsAsync()
        {
            var response = await _httpClient.PostAsync("/sdapi/v1/refresh-checkpoints", null);
            response.EnsureSuccessStatusCode();
        }
    
        public async Task<List<string>> GetAvailableSamplersAsync(){
            //var response = await _httpClient.GetAsync("http://127.0.0.1:7861/sdapi/v1/samplers");
            var response = await _httpClient.GetAsync("/sdapi/v1/samplers");
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

        public async Task<string> StartDownloadModelAsync(string modelUrl, string? checksum = null)
        {
            var requestData = new
            {
                url = modelUrl,
                checksum = checksum
            };

            Debug.WriteLine($"Starting model download from URL: {modelUrl} with checksum: {checksum}");

            var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/sdapi/v1/download-model", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to start model download: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var status = root.TryGetProperty("status", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() : "started";
            if (status == "exists")
            {
                throw new InvalidOperationException("File already exists on server.");
            }
            var idProp = root.TryGetProperty("download_id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrEmpty(idProp))
                throw new Exception("download_id not returned by server");
            return idProp!;
        }

        public class DownloadProgress
        {
            public string Status { get; set; } = "in_progress"; // in_progress | completed | failed
            public float Progress { get; set; }
            public long DownloadedBytes { get; set; }
            public long TotalBytes { get; set; }
            public string? Error { get; set; }
            public string? FilePath { get; set; }
        }

        public async Task<DownloadProgress> GetDownloadModelProgressAsync(string downloadId)
        {
            var response = await _httpClient.GetAsync($"/sdapi/v1/download-model/progress/{downloadId}");
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get download progress: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;
            return new DownloadProgress
            {
                Status = root.GetProperty("status").GetString() ?? "in_progress",
                Progress = root.TryGetProperty("progress", out var p) ? p.GetSingle() : 0f,
                DownloadedBytes = root.TryGetProperty("downloaded_bytes", out var db) && db.TryGetInt64(out var dbv) ? dbv : 0,
                TotalBytes = root.TryGetProperty("total_bytes", out var tb) && tb.TryGetInt64(out var tbv) ? tbv : 0,
                Error = root.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String ? e.GetString() : null,
                FilePath = root.TryGetProperty("file_path", out var fp) && fp.ValueKind == JsonValueKind.String ? fp.GetString() : null
            };
        }
        
    }
}
