namespace MySkills.Api.Models;

public class Achievement
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;
    // Example: "FIRST_SESSION", "STREAK_7", "XP_100"

    public string Title { get; set; } = string.Empty;

    public DateTime UnlockedAt { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
