using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Net.Http;
using OpenAI.Chat;
using System.Threading.Tasks;
using OpenAI;

namespace myApp.Services;


public class PromptAssistantService
{
    private readonly OpenAIClient _client;

    public PromptAssistantService(string apiKey)
    {
    
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.ai.it.ufl.edu")
        };
        var credentials = new ApiKeyCredential(apiKey);
        _client = new OpenAIClient(credentials, options);
    }

    public async Task<string> EnhancePromptAsync(string userPrompt)
    {
        var chatMessages = new List<ChatMessage>
        {
            new SystemChatMessage("You are a Stable Diffusion prompt engineer. Expand and enhance prompts for beautiful results."),
            new UserChatMessage("Generate a single-line prompt suitable for AI image generation. Do not include bullets, suggestions, or explanations. Your prompt is: ", userPrompt)
        };

        ChatClient chatClient = _client.GetChatClient("llama-3.1-8b-instruct");

        ChatCompletionOptions chatCompletionOptions = new ChatCompletionOptions();

        try
        {
            ChatCompletion response = await chatClient.CompleteChatAsync(chatMessages);

            if (response.Content.Count > 0)
            {
                return response.Content[0].Text;
            }

            return "Error: Prompt enhancement failed, no content received";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An API error occurred during prompt enhancement: {ex.Message}");
            return $"Error enhancing prompt: {ex.Message}";
        }

    }
    
}