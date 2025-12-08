using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace PersonalMedia.Services;

public class RunPodImageGenerationService : IRunPodImageGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public RunPodImageGenerationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<JobSubmissionResult> SubmitJobAsync(string prompt, string baseImageUrl = null)
    {
        try
        {
            var apiKey = _configuration["RunPod:ApiKey"];
            var endpointId = _configuration["RunPod:EndpointId"];
            var webhookUrl = _configuration["RunPod:WebhookUrl"];

            if (string.IsNullOrEmpty(apiKey))
            {
                return new JobSubmissionResult
                {
                    Success = false,
                    ErrorMessage = "RunPod API key not configured",
                    ShouldRetry = false
                };
            }

            if (string.IsNullOrEmpty(endpointId))
            {
                return new JobSubmissionResult
                {
                    Success = false,
                    ErrorMessage = "RunPod endpoint ID not configured",
                    ShouldRetry = false
                };
            }

            if (string.IsNullOrEmpty(webhookUrl))
            {
                return new JobSubmissionResult
                {
                    Success = false,
                    ErrorMessage = "RunPod webhook URL not configured",
                    ShouldRetry = false
                };
            }

            var runPodUrl = $"https://api.runpod.ai/v2/{endpointId}/run";

            // Build input parameters
            var inputParams = new Dictionary<string, object>
            {
                { "prompt", prompt }
            };

            // Add base image URL if provided (for img2img)
            if (!string.IsNullOrEmpty(baseImageUrl))
            {
                inputParams["image"] = baseImageUrl;
            }

            // Add model parameters from configuration
            var modelParams = _configuration.GetSection("RunPod:ModelParameters");
            foreach (var param in modelParams.GetChildren())
            {
                if (int.TryParse(param.Value, out int intValue))
                {
                    inputParams[param.Key] = intValue;
                }
                else if (double.TryParse(param.Value, out double doubleValue))
                {
                    inputParams[param.Key] = doubleValue;
                }
                else
                {
                    inputParams[param.Key] = param.Value;
                }
            }

            var requestBody = new
            {
                input = inputParams,
                webhook = webhookUrl
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.PostAsync(runPodUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new JobSubmissionResult
                {
                    Success = false,
                    ErrorMessage = $"RunPod API request failed: {response.StatusCode} - {errorContent}",
                    ShouldRetry = response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                                  response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RunPodSubmissionResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Id != null)
            {
                return new JobSubmissionResult
                {
                    Success = true,
                    JobId = result.Id
                };
            }

            return new JobSubmissionResult
            {
                Success = false,
                ErrorMessage = "No job ID in RunPod response",
                ShouldRetry = true
            };
        }
        catch (Exception ex)
        {
            return new JobSubmissionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ShouldRetry = true
            };
        }
    }

    private class RunPodSubmissionResponse
    {
        public string Id { get; set; }
        public string Status { get; set; }
    }
}
