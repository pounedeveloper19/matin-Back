namespace MatinPower.Server.Models;

public class OtpCode
{
    public int Id { get; set; }
    public string Mobile { get; set; } = null!;
    public string Code { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }
}
