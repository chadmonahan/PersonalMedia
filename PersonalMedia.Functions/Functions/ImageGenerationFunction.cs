using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PersonalMedia.Core.Entities;
using PersonalMedia.Data;
using PersonalMedia.Services;

namespace PersonalMedia.Functions.Functions;

public class ImageGenerationFunction
{
    private readonly ILogger<ImageGenerationFunction> _logger;
    private readonly PersonalMediaDbContext _context;
    private readonly IRunPodImageGenerationService _runPodService;
    private readonly IAzureStorageService _azureStorageService;

    public ImageGenerationFunction(
        ILogger<ImageGenerationFunction> logger,
        PersonalMediaDbContext context,
        IRunPodImageGenerationService runPodService,
        IAzureStorageService azureStorageService)
    {
        _logger = logger;
        _context = context;
        _runPodService = runPodService;
        _azureStorageService = azureStorageService;
    }

    // [Function("GenerateNightlyImages")] // Disabled for testing
    public async Task Run([TimerTrigger("0 0 3 * * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation("Starting nightly image generation at: {Time}", DateTime.Now);

        try
        {
            await ProcessPendingImages();
            await CreateNewImageSets();

            _logger.LogInformation("Nightly image generation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during nightly image generation");
        }
    }

    private async Task ProcessPendingImages()
    {
        var pendingImages = await _context.MediaItems
            .Include(m => m.BasePersonImage)
            .Include(m => m.GenerationParameters)
            .Where(m => m.GenerationStatus == GenerationStatus.Pending ||
                       m.GenerationStatus == GenerationStatus.Retrying)
            .ToListAsync();

        _logger.LogInformation("Found {Count} pending images to process", pendingImages.Count);

        foreach (var mediaItem in pendingImages)
        {
            await ProcessMediaItem(mediaItem);
        }
    }

    private async Task ProcessMediaItem(MediaItem mediaItem)
    {
        var settings = await _context.GenerationSettings.FirstOrDefaultAsync();

        if (mediaItem.RetryCount >= (settings?.MaxRetryAttempts ?? 3))
        {
            _logger.LogWarning("Max retry attempts reached for MediaItem {Id}", mediaItem.Id);
            mediaItem.GenerationStatus = GenerationStatus.Failed;
            mediaItem.ErrorMessage = "Maximum retry attempts exceeded";
            await _context.SaveChangesAsync();
            return;
        }

        try
        {
            mediaItem.GenerationStatus = GenerationStatus.InProgress;
            mediaItem.GenerationStartedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Submit job to RunPod (async - webhook will complete it later)
            var result = await _runPodService.SubmitJobAsync(
                mediaItem.GenerationPrompt,
                mediaItem.BasePersonImage?.AzureStorageUrl);

            if (result.Success)
            {
                // Store job ID and wait for webhook
                mediaItem.RunPodJobId = result.JobId;
                mediaItem.JobSubmittedDate = DateTime.UtcNow;
                // Status remains InProgress until webhook arrives

                _logger.LogInformation("Submitted RunPod job {JobId} for MediaItem {Id}",
                    result.JobId, mediaItem.Id);
            }
            else
            {
                // Submission failed - mark for retry
                mediaItem.RetryCount++;
                mediaItem.GenerationStatus = result.ShouldRetry
                    ? GenerationStatus.Retrying
                    : GenerationStatus.Failed;
                mediaItem.ErrorMessage = result.ErrorMessage;

                _logger.LogWarning("Failed to submit RunPod job for MediaItem {Id}: {Error}",
                    mediaItem.Id, result.ErrorMessage);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MediaItem {Id}", mediaItem.Id);

            mediaItem.RetryCount++;
            mediaItem.GenerationStatus = GenerationStatus.Retrying;
            mediaItem.ErrorMessage = ex.Message;

            await _context.SaveChangesAsync();
        }
    }

    private async Task CreateNewImageSets()
    {
        var settings = await _context.GenerationSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            _logger.LogWarning("No generation settings found");
            return;
        }

        var today = DateTime.UtcNow.Date;
        if (settings.LastGenerationDate.Date >= today)
        {
            _logger.LogInformation("Images already generated for today");
            return;
        }

        var random = new Random();
        var basePersonImages = await _context.BasePersonImages
            .Where(b => b.IsActive)
            .ToListAsync();

        if (!basePersonImages.Any())
        {
            _logger.LogWarning("No active base person images found");
            return;
        }

        var parameterOptions = await _context.ParameterOptions
            .Where(p => p.IsActive)
            .ToListAsync();

        var lastDisplayOrder = await _context.MediaSets
            .MaxAsync(s => (int?)s.DisplayOrder) ?? 0;

        for (int setIndex = 0; setIndex < settings.DailySetsCount; setIndex++)
        {
            var mediaSet = new MediaSet
            {
                CreatedDate = DateTime.UtcNow,
                DisplayOrder = lastDisplayOrder + setIndex + 1,
                IsActive = true,
                MediaItems = new List<MediaItem>()
            };

            var imagesInSet = random.Next(settings.ImagesPerSetMin, settings.ImagesPerSetMax + 1);

            for (int imageIndex = 0; imageIndex < imagesInSet; imageIndex++)
            {
                var basePersonImage = basePersonImages[random.Next(basePersonImages.Count)];
                var prompt = GeneratePrompt(parameterOptions, settings, random);

                var mediaItem = new MediaItem
                {
                    MediaType = MediaType.Image,
                    CreatedDate = DateTime.UtcNow,
                    DisplayOrder = imageIndex,
                    IsActive = true,
                    BasePersonImageId = basePersonImage.Id,
                    GenerationPrompt = prompt,
                    GenerationStatus = GenerationStatus.Pending,
                    RetryCount = 0,
                    GenerationParameters = CreateGenerationParameters(parameterOptions, random)
                };

                mediaSet.MediaItems.Add(mediaItem);
            }

            _context.MediaSets.Add(mediaSet);
        }

        settings.LastGenerationDate = DateTime.UtcNow;
        settings.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created {Count} new media sets with images", settings.DailySetsCount);
    }

    private string GeneratePrompt(List<ParameterOption> options, GenerationSettings settings, Random random)
    {
        var selectedParams = new Dictionary<ParameterCategory, string>();

        foreach (ParameterCategory category in Enum.GetValues(typeof(ParameterCategory)))
        {
            var categoryOptions = options.Where(o => o.Category == category).ToList();
            if (categoryOptions.Any())
            {
                var selected = categoryOptions[random.Next(categoryOptions.Count)];
                selectedParams[category] = selected.Value;
            }
        }

        var prompt = $"A photorealistic portrait of a person";

        if (selectedParams.ContainsKey(ParameterCategory.Setting))
            prompt += $" in a {selectedParams[ParameterCategory.Setting]}";

        if (selectedParams.ContainsKey(ParameterCategory.Activity))
            prompt += $", {selectedParams[ParameterCategory.Activity].ToLower()}";

        if (selectedParams.ContainsKey(ParameterCategory.TimeOfDay))
            prompt += $", during {selectedParams[ParameterCategory.TimeOfDay].ToLower()}";

        if (selectedParams.ContainsKey(ParameterCategory.Weather))
            prompt += $", {selectedParams[ParameterCategory.Weather].ToLower()} weather";

        if (selectedParams.ContainsKey(ParameterCategory.Mood))
            prompt += $". {selectedParams[ParameterCategory.Mood]} mood";

        if (selectedParams.ContainsKey(ParameterCategory.Clothing))
            prompt += $". Wearing {selectedParams[ParameterCategory.Clothing].ToLower()} attire";

        if (selectedParams.ContainsKey(ParameterCategory.Style))
            prompt += $". {selectedParams[ParameterCategory.Style]} style";

        prompt += $". {settings.ModestyLevel}. High quality, professional photography.";

        return prompt;
    }

    private List<GenerationParameter> CreateGenerationParameters(List<ParameterOption> options, Random random)
    {
        var parameters = new List<GenerationParameter>();

        foreach (ParameterCategory category in Enum.GetValues(typeof(ParameterCategory)))
        {
            var categoryOptions = options.Where(o => o.Category == category).ToList();
            if (categoryOptions.Any())
            {
                var selected = categoryOptions[random.Next(categoryOptions.Count)];
                parameters.Add(new GenerationParameter
                {
                    Category = category,
                    Value = selected.Value
                });
            }
        }

        return parameters;
    }
}
