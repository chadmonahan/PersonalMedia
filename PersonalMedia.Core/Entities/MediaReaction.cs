namespace PersonalMedia.Core.Entities;

public class MediaReaction
{
    public int Id { get; set; }
    public int MediaItemId { get; set; }
    public ReactionType ReactionType { get; set; }
    public DateTime CreatedDate { get; set; }

    public MediaItem MediaItem { get; set; }
}

public enum ReactionType
{
    Like = 1,
    Dislike = 2
}
