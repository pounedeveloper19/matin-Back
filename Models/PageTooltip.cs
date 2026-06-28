namespace MatinPower.Server.Models;

public class PageTooltip
{
    public int    Id        { get; set; }
    public string PageKey   { get; set; } = null!;
    public string FieldKey  { get; set; } = null!;
    public string Title     { get; set; } = null!;
    public string Content   { get; set; } = null!;
    public bool   IsActive  { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
