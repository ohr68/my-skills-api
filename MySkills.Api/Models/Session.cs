namespace MySkills.Api.Models;

public class Session
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty; 
    // Example: "LeetCode", "Project", "Reading", "SystemDesign"

    public string Difficulty { get; set; } = string.Empty; 
    // Example: "Easy", "Medium", "Hard"

    public int XpEarned { get; set; }

    public DateOnly Date { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
