using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersonalMedia.Core.Entities;
using PersonalMedia.Data;
using PersonalMedia.Services;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PersonalMedia.Functions.Functions;

public class RunPodWebhookFunction
{
    private readonly ILogger<RunPodWebhookFunction> _logger;
    private readonly PersonalMediaDbContext _context;
    private readonly IAzureStorageService _azureStorageService;
    private readonly IConfiguration _configuration;

    public RunPodWebhookFunction(
        ILogger<RunPodWebhookFunction> logger,
        PersonalMediaDbContext context,
        IAzureStorageService azureStorageService,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _azureStorageService = azureStorageService;
        _configuration = configuration;
    }

    [Function("RunPodWebhook")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "runpod-webhook")]
        HttpRequestData req)
    {
        try
        {
            // Read the request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            _logger.LogInformation("Received RunPod webhook");

            // Validate webhook signature
            var webhookSecret = _configuration["RunPod:WebhookSecret"];
            if (!string.IsNullOrEmpty(webhookSecret))
            {
                if (!ValidateWebhookSignature(req, requestBody, webhookSecret))
                {
                    _logger.LogWarning("Invalid webhook signature");
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid signature");
                    return badResponse;
                }
            }

            // Parse the webhook payload
            var payload = JsonSerializer.Deserialize<RunPodWebhookPayload>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload == null || string.IsNullOrEmpty(payload.Id))
            {
                _logger.LogWarning("Invalid webhook payload");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid payload");
                return badResponse;
            }

            // Log the webhook
            var webhookLog = new RunPodWebhookLog
            {
                JobId = payload.Id,
                ReceivedDate = DateTime.UtcNow,
                Status = payload.Status,
                RawPayload = requestBody,
                WasProcessed = false
            };

            _context.RunPodWebhookLogs.Add(webhookLog);
            await _context.SaveChangesAsync();

            // Process the webhook
            await ProcessWebhook(payload, webhookLog);

            // Return 200 OK immediately (critical for RunPod retry logic)
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("OK");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RunPod webhook");

            // Return 200 OK even on error to prevent RunPod retries for processing errors
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Error logged");
            return response;
        }
    }

    private bool ValidateWebhookSignature(HttpRequestData req, string payload, string secret)
    {
        try
        {
            if (!req.Headers.TryGetValues("X-Webhook-Signature", out var signatureValues))
            {
                return false;
            }

            var receivedSignature = signatureValues.FirstOrDefault();
            if (string.IsNullOrEmpty(receivedSignature))
            {
                return false;
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var expectedSignature = Convert.ToBase64String(hash);

            return receivedSignature == expectedSignature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating webhook signature");
            return false;
        }
    }

    private async Task ProcessWebhook(RunPodWebhookPayload payload, RunPodWebhookLog webhookLog)
    {
        try
        {
            // Find MediaItem by RunPodJobId
            var mediaItem = await _context.MediaItems
                .Include(m => m.BasePersonImage)
                .FirstOrDefaultAsync(m => m.RunPodJobId == payload.Id);

            if (mediaItem == null)
            {
                _logger.LogWarning("Received webhook for unknown job ID: {JobId}", payload.Id);
                webhookLog.ProcessingError = "No MediaItem found for job ID";
                await _context.SaveChangesAsync();
                return;
            }

            webhookLog.MediaItemId = mediaItem.Id;

            // Idempotency check
            if (mediaItem.WebhookReceivedDate.HasValue)
            {
                _logger.LogInformation("Duplicate webhook for job {JobId} - already processed", payload.Id);
                webhookLog.WasProcessed = true;
                await _context.SaveChangesAsync();
                return;
            }

            mediaItem.WebhookReceivedDate = DateTime.UtcNow;
            mediaItem.ExecutionTimeMs = payload.ExecutionTime;
            mediaItem.RawWebhookPayload = JsonSerializer.Serialize(payload);

            if (payload.Status == "COMPLETED" && payload.Output != null)
            {
                await ProcessCompletedJob(mediaItem, payload);
                webhookLog.WasProcessed = true;
            }
            else if (payload.Status == "FAILED")
            {
                ProcessFailedJob(mediaItem, payload);
                webhookLog.WasProcessed = true;
            }
            else
            {
                _logger.LogWarning("Received webhook with unexpected status: {Status} for job {JobId}",
                    payload.Status, payload.Id);
                webhookLog.ProcessingError = $"Unexpected status: {payload.Status}";
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook for job {JobId}", payload.Id);
            webhookLog.ProcessingError = ex.Message;
            await _context.SaveChangesAsync();
            throw;
        }
    }

    private async Task ProcessCompletedJob(MediaItem mediaItem, RunPodWebhookPayload payload)
    {
        try
        {
            // Extract image URL from output
            var imageUrl = ExtractImageUrl(payload.Output);

            if (string.IsNullOrEmpty(imageUrl))
            {
                _logger.LogWarning("No image URL found in webhook output for job {JobId}", payload.Id);
                mediaItem.GenerationStatus = GenerationStatus.Retrying;
                mediaItem.RetryCount++;
                mediaItem.ErrorMessage = "No image URL in webhook output";
                return;
            }

            // Download image from RunPod and upload to Azure Storage
            var fileName = $"generated/{Guid.NewGuid()}.jpg";
            var storageUrl = await _azureStorageService.UploadFromUrlAsync(imageUrl, fileName);

            mediaItem.AzureStorageUrl = storageUrl;
            mediaItem.ThumbnailUrl = storageUrl;
            mediaItem.GenerationStatus = GenerationStatus.Completed;
            mediaItem.GenerationCompletedDate = DateTime.UtcNow;
            mediaItem.ErrorMessage = null;

            // Increment base image usage count
            if (mediaItem.BasePersonImage != null)
            {
                mediaItem.BasePersonImage.UsageCount++;
            }

            _logger.LogInformation("Successfully processed completed image for MediaItem {Id}", mediaItem.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download/upload image for job {JobId}", payload.Id);
            mediaItem.GenerationStatus = GenerationStatus.Retrying;
            mediaItem.RetryCount++;
            mediaItem.ErrorMessage = $"Image download failed: {ex.Message}";
        }
    }

    private void ProcessFailedJob(MediaItem mediaItem, RunPodWebhookPayload payload)
    {
        mediaItem.GenerationStatus = GenerationStatus.Failed;
        mediaItem.ErrorMessage = payload.Output?.Error ?? "RunPod job failed";

        _logger.LogWarning("RunPod job {JobId} failed for MediaItem {Id}: {Error}",
            payload.Id, mediaItem.Id, mediaItem.ErrorMessage);
    }

    private string ExtractImageUrl(RunPodOutput output)
    {
        if (output == null)
        {
            return null;
        }

        // Try different possible property names
        if (!string.IsNullOrEmpty(output.ImageUrl))
        {
            return output.ImageUrl;
        }

        if (!string.IsNullOrEmpty(output.Image))
        {
            return output.Image;
        }

        if (!string.IsNullOrEmpty(output.Url))
        {
            return output.Url;
        }

        // If image is base64, we would handle it differently
        // For now, just return null if no URL found
        return null;
    }

    private class RunPodWebhookPayload
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public RunPodOutput Output { get; set; }
        public int? DelayTime { get; set; }
        public int? ExecutionTime { get; set; }
    }

    private class RunPodOutput
    {
        public string ImageUrl { get; set; }
        public string Image { get; set; }
        public string Url { get; set; }
        public string Error { get; set; }
        public int? Seed { get; set; }
    }
}
