namespace PersonalMedia.Core.Entities;

public class MediaSet
{
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }

    public ICollection<MediaItem> MediaItems { get; set; }
}
