namespace PersonalMedia.Core.Entities;

public class BasePersonImage
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string AzureStorageUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public int UsageCount { get; set; }

    public ICollection<MediaItem> MediaItems { get; set; }
}
