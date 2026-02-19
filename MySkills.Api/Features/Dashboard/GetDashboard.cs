using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MySkills.Api.Data;
using MySkills.Api.Utils;

namespace MySkills.Api.Features.Dashboard;

public static class GetDashboard
{
    public static void MapGetDashboard(this WebApplication app)
    {
        app.MapGet("/dashboard", Handle).RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        AppDbContext db,
        ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdClaim is null)
            return Results.Unauthorized();

        var userId = Guid.Parse(userIdClaim);

        var existingUser = await db.Users
            .Include(u => u.Achievements)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (existingUser is null)
            return Results.NotFound("User not found");

        var recentActivities = await db.Activities
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CompletedAt)
            .Take(5)
            .Select(a => new
            {
                a.Id,
                a.Type,
                a.XpEarned,
                a.CompletedAt
            })
            .ToListAsync();

        var level = LevelUtils.CalculateLevel(existingUser.TotalXp);
        var xpToNextLevel = LevelUtils.CalculateXpToNextLevel(existingUser.TotalXp);

        var achievements = existingUser.Achievements!
            .OrderByDescending(a => a.UnlockedAt)
            .Select(a => new AchievementDto(a.Code, a.Title, a.UnlockedAt))
            .ToList();

        return Results.Ok(new
        {
            totalXp = existingUser.TotalXp,
            currentLevel = level,
            currentStreak = existingUser.CurrentStreak,
            longestStreak = existingUser.LongestStreak,
            xpToNextLevel,
            recentActivities,
            achievements,
        });
    }
}