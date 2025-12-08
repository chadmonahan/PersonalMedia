namespace PersonalMedia.Core.Entities;

public class ParameterOption
{
    public int Id { get; set; }
    public ParameterCategory Category { get; set; }
    public string Value { get; set; }
    public bool IsActive { get; set; }
    public int Weight { get; set; }
}
