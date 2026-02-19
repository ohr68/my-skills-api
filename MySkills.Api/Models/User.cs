using System.Diagnostics;

namespace MySkills.Api.Models;

public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public int TotalXp { get; set; } = 0;

    public virtual ICollection<Activity>? Activities { get; set; }
}