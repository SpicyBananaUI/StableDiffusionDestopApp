using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace myApp.Services
{
    /// Service for interacting with the Gradio-to-Avalonia translation layer API.
    public class TranslationLayerService
    {
        private readonly HttpClient _httpClient;

        public TranslationLayerService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(App.AppConfig.RemoteAddress),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

            /// Component tree structure as returned from the backend.
        public class ComponentTreeResponse
        {
            public bool Active { get; set; }
            public string? Message { get; set; }
            public ComponentTree Tree { get; set; } = new();
        }

        public class ComponentTree
        {
            public Dictionary<string, ExtensionTree> Extensions { get; set; } = new();
            public List<string> supported_types { get; set; } = new();
            public int total_extensions { get; set; }
        }

        public class ExtensionTree
        {
            public List<string> root_nodes { get; set; } = new();
            public Dictionary<string, ComponentNode> Components { get; set; } = new();
            public bool? Supported { get; set; }
            public int component_count { get; set; }
            public List<string> component_types { get; set; } = new();
            public List<string> unsupported_types { get; set; } = new();
        }

        public class ComponentNode
        {
            public string Id { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public bool Supported { get; set; }
            public Dictionary<string, JsonElement> Props { get; set; } = new();
            public Dictionary<string, JsonElement> Events { get; set; } = new();
            public string? ParentId { get; set; }
            public List<string> Children { get; set; } = new();
        }

        public class ComponentValueResponse
        {
            public string NodeId { get; set; } = string.Empty;
            public JsonElement Value { get; set; }
        }

        public class TranslationLayerStatus
        {
            public bool Active { get; set; }
            public int ComponentCount { get; set; }
            public int RootNodes { get; set; }
        }

        public class SupportedTypesResponse
        {
            public List<string> SupportedTypes { get; set; } = new();
            public List<string> EncounteredTypes { get; set; } = new();
            public List<string> UnsupportedTypes { get; set; } = new();
        }

        public class ExtensionValuesResponse
        {
            public bool Active { get; set; }
            public string? Message { get; set; }
            public Dictionary<string, ExtensionArgs> Values { get; set; } = new();
        }

        public class ExtensionArgs
        {
            public List<JsonElement> Args { get; set; } = new();
        }

            /// Get the list of supported component types.
        public async Task<SupportedTypesResponse?> GetSupportedTypesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/translation-layer/supported-types");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<SupportedTypesResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        public class ExtensionWithCompatibility
        {
            public string Name { get; set; } = string.Empty;
            public string Remote { get; set; } = string.Empty;
            public string Branch { get; set; } = string.Empty;
            public string CommitHash { get; set; } = string.Empty;
            public int CommitDate { get; set; }
            public string Version { get; set; } = string.Empty;
            public bool Enabled { get; set; }
            public TranslationLayerInfo TranslationLayer { get; set; } = new();
        }

        public class TranslationLayerInfo
        {
            public bool? Supported { get; set; }
            public List<string> ComponentTypes { get; set; } = new();
            public List<string> UnsupportedTypes { get; set; } = new();
            public int ComponentCount { get; set; }
        }

        /// Get extensions list with translation layer compatibility information.
        public async Task<List<ExtensionWithCompatibility>> GetExtensionsWithCompatibilityAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/translation-layer/extensions");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ExtensionWithCompatibility>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<ExtensionWithCompatibility>();
            }
            catch
            {
                return new List<ExtensionWithCompatibility>();
            }
        }

            /// Get the full component tree from the backend.
        public async Task<ComponentTreeResponse> GetComponentTreeAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/translation-layer/component-tree");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ComponentTreeResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new ComponentTreeResponse();
            }
            catch (Exception ex)
            {
                // Return empty tree on error
                return new ComponentTreeResponse
                {
                    Active = false,
                    Message = $"Failed to fetch component tree: {ex.Message}"
                };
            }
        }

            /// Get a specific component by ID.
        public async Task<ComponentNode?> GetComponentAsync(string nodeId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/translation-layer/component/{nodeId}");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("node", out var nodeElement))
                {
                    return JsonSerializer.Deserialize<ComponentNode>(nodeElement.GetRawText(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

            /// Get the current value of a component.
        public async Task<JsonElement?> GetComponentValueAsync(string nodeId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/translation-layer/component/{nodeId}/value");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("value", out var valueElement))
                {
                    return valueElement;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

            /// Set the value of a component.
        public async Task<bool> SetComponentValueAsync(string nodeId, object value)
        {
            try
            {
                var payload = new { value };
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"/translation-layer/component/{nodeId}/value", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

            /// Trigger an event on a component.
        public async Task<bool> TriggerComponentEventAsync(string nodeId, string eventName, object? data = null)
        {
            try
            {
                object payload;
                if (data != null) payload = new { data };
                else payload = new { };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8
                );
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                var response = await _httpClient.PostAsync($"/translation-layer/component/{nodeId}/event/{eventName}", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

            /// Get the status of the translation layer.
        public async Task<TranslationLayerStatus?> GetStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/translation-layer/status");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TranslationLayerStatus>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        /// Get extension component values in alwayson_scripts format.
        public async Task<ExtensionValuesResponse> GetExtensionValuesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/translation-layer/extension-values");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ExtensionValuesResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new ExtensionValuesResponse();
            }
            catch (Exception ex)
            {
                return new ExtensionValuesResponse
                {
                    Active = false,
                    Message = $"Failed to fetch extension values: {ex.Message}"
                };
            }
        }
    }
}

