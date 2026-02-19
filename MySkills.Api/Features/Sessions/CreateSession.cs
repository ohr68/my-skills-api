using Microsoft.EntityFrameworkCore;
using MySkills.Api.Data;
using MySkills.Api.Interfaces;
using MySkills.Api.Models;

namespace MySkills.Api.Features.Sessions;

public static class CreateSession
{
    public static void MapCreateSession(this WebApplication app)
    {
        app.MapPost("/users/{userId:guid}/sessions", Handle);
    }

    private static async Task<IResult> Handle(
        Guid userId,
        CreateSessionDto dto,
        AppDbContext db,
        IStreakService streakService,
        IAchievementService achievementService)
    {
        var user = await db.Users
            .Include(u => u.Achievements)
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return Results.NotFound();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var session = new Session
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Type = dto.Type,
            Difficulty = dto.Difficulty,
            XpEarned = dto.XpEarned,
            Date = today,
            UserId = userId
        };

        user.TotalXp += dto.XpEarned;

        streakService.Update(user, today);

        var newAchievements = achievementService.Evaluate(user);

        db.Sessions.Add(session);
        db.Achievements.AddRange(newAchievements);

        await db.SaveChangesAsync();

        return Results.Ok(session);
    }
}

public abstract record CreateSessionDto(
    string Title,
    string Type,
    string Difficulty,
    int XpEarned
);