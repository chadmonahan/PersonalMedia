namespace PersonalMedia.Blazor.Models;

public class ReactionToggleEventArgs
{
    public int MediaItemId { get; set; }
    public string ReactionType { get; set; } = string.Empty;
}

public class ImageClickEventArgs
{
    public string ImageUrl { get; set; } = string.Empty;
    public int MediaItemId { get; set; }
    public bool HasLike { get; set; }
    public bool HasDislike { get; set; }
}
