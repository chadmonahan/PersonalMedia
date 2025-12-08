namespace PersonalMedia.Core.Entities;

public class MediaItem
{
    public int Id { get; set; }
    public int MediaSetId { get; set; }
    public MediaType MediaType { get; set; }
    public string AzureStorageUrl { get; set; }
    public string ThumbnailUrl { get; set; }
    public DateTime CreatedDate { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }

    public int? BasePersonImageId { get; set; }
    public string GenerationPrompt { get; set; }
    public GenerationStatus GenerationStatus { get; set; }
    public DateTime? GenerationStartedDate { get; set; }
    public DateTime? GenerationCompletedDate { get; set; }
    public int RetryCount { get; set; }
    public string ErrorMessage { get; set; }

    // RunPod Integration Fields
    public string RunPodJobId { get; set; }
    public DateTime? JobSubmittedDate { get; set; }
    public DateTime? WebhookReceivedDate { get; set; }
    public int? ExecutionTimeMs { get; set; }
    public string RawWebhookPayload { get; set; }

    public MediaSet MediaSet { get; set; }
    public BasePersonImage BasePersonImage { get; set; }
    public ICollection<MediaReaction> Reactions { get; set; }
    public ICollection<GenerationParameter> GenerationParameters { get; set; }
}

public enum MediaType
{
    Image = 1,
    Video = 2
}

public enum GenerationStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    Retrying = 5
}
