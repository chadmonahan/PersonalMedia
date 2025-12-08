namespace PersonalMedia.Services;

public interface IRunPodImageGenerationService
{
    Task<JobSubmissionResult> SubmitJobAsync(string prompt, string baseImageUrl = null);
}

public class JobSubmissionResult
{
    public bool Success { get; set; }
    public string JobId { get; set; }
    public string ErrorMessage { get; set; }
    public bool ShouldRetry { get; set; }
}
