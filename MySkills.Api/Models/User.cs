using System.Diagnostics;

namespace MySkills.Api.Models;

public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public int TotalXp { get; set; } = 0;
    
    
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }

    public DateOnly? LastActivityDate { get; set; }

    public virtual ICollection<Session>? Sessions { get; set; }
    public virtual ICollection<Activity>? Activities { get; set; }
    public virtual ICollection<Achievement>? Achievements { get; set; }
}