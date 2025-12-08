namespace PersonalMedia.Core.Entities;

public class GenerationSettings
{
    public int Id { get; set; }
    public int DailySetsCount { get; set; } = 5;
    public int ImagesPerSetMin { get; set; } = 3;
    public int ImagesPerSetMax { get; set; } = 5;
    public int MaxRetryAttempts { get; set; } = 3;
    public string ModestyLevel { get; set; } = "Family Friendly";
    public DateTime LastGenerationDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
