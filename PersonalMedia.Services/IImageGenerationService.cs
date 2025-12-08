namespace PersonalMedia.Services;

public interface IImageGenerationService
{
    Task<ImageGenerationResult> GenerateImageAsync(string prompt, string baseImageUrl = null);
}

public class ImageGenerationResult
{
    public bool Success { get; set; }
    public string ImageUrl { get; set; }
    public string ErrorMessage { get; set; }
    public bool ShouldRetry { get; set; }
}
