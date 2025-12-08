namespace PersonalMedia.Core.Entities;

public class GenerationParameter
{
    public int Id { get; set; }
    public int MediaItemId { get; set; }
    public ParameterCategory Category { get; set; }
    public string Value { get; set; }

    public MediaItem MediaItem { get; set; }
}

public enum ParameterCategory
{
    Setting = 1,
    Mood = 2,
    Activity = 3,
    Clothing = 4,
    TimeOfDay = 5,
    Weather = 6,
    Style = 7
}
