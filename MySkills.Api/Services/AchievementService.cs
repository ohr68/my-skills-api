using MySkills.Api.Interfaces;
using MySkills.Api.Models;

namespace MySkills.Api.Services;

public class AchievementService : IAchievementService
{
    public List<Achievement> Evaluate(User user)
    {
        var achievements = new List<Achievement>();

        if (user.Sessions!.Count == 1 && !HasAchievement("FIRST_SESSION"))
        {
            achievements.Add(Create(user.Id, "FIRST_SESSION", "First Step 🚀"));
        }

        if (user.CurrentStreak >= 3 && !HasAchievement("STREAK_3"))
        {
            achievements.Add(Create(user.Id, "STREAK_3", "On Fire 🔥"));
        }

        if (user.CurrentStreak >= 7 && !HasAchievement("STREAK_7"))
        {
            achievements.Add(Create(user.Id, "STREAK_7", "Consistency Master 💪"));
        }

        if (user.TotalXp >= 100 && !HasAchievement("XP_100"))
        {
            achievements.Add(Create(user.Id, "XP_100", "100 XP Achieved 🎯"));
        }

        if (user.TotalXp >= 500 && !HasAchievement("XP_500"))
        {
            achievements.Add(Create(user.Id, "XP_500", "500 XP Elite 🏆"));
        }

        return achievements;

        bool HasAchievement(string code) =>
            user.Achievements!.Any(a => a.Code == code);
    }

    private static Achievement Create(Guid userId, string code, string title) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Code = code,
            Title = title,
            UnlockedAt = DateTime.UtcNow,
            UserId = userId,
        };
}
