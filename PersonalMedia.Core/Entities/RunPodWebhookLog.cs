namespace PersonalMedia.Core.Entities;

public class RunPodWebhookLog
{
    public int Id { get; set; }
    public string JobId { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string Status { get; set; }
    public string RawPayload { get; set; }
    public bool WasProcessed { get; set; }
    public string ProcessingError { get; set; }
    public int? MediaItemId { get; set; }
}
