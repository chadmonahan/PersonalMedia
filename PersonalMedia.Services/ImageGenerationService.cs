using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace PersonalMedia.Services;

public class ImageGenerationService : IImageGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ImageGenerationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<ImageGenerationResult> GenerateImageAsync(string prompt, string baseImageUrl = null)
    {
        try
        {
            var apiKey = _configuration["ImageGeneration:ApiKey"];
            var endpoint = _configuration["ImageGeneration:Endpoint"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpoint))
            {
                return new ImageGenerationResult
                {
                    Success = false,
                    ErrorMessage = "Image generation API not configured",
                    ShouldRetry = false
                };
            }

            var requestBody = new
            {
                prompt = prompt,
                n = 1,
                size = "1024x1024",
                quality = "standard",
                style = "natural"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.PostAsync(endpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new ImageGenerationResult
                {
                    Success = false,
                    ErrorMessage = $"API request failed: {response.StatusCode} - {errorContent}",
                    ShouldRetry = response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ImageGenerationResponse>(responseContent);

            if (result?.Data?.Length > 0)
            {
                return new ImageGenerationResult
                {
                    Success = true,
                    ImageUrl = result.Data[0].Url
                };
            }

            return new ImageGenerationResult
            {
                Success = false,
                ErrorMessage = "No image URL in response",
                ShouldRetry = true
            };
        }
        catch (Exception ex)
        {
            return new ImageGenerationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ShouldRetry = true
            };
        }
    }

    private class ImageGenerationResponse
    {
        public ImageData[] Data { get; set; }
    }

    private class ImageData
    {
        public string Url { get; set; }
    }
}
