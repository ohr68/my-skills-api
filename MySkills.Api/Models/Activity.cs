namespace MySkills.Api.Models;

public class Activity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public string? Type { get; set; }
    public int XpEarned { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public virtual User? User { get; set; }
}