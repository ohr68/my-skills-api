using System.Security.Claims;
using MySkills.Api.Data;
using MySkills.Api.Models;
using MySkills.Api.Utils;

namespace MySkills.Api.Features.Activities;

public static class CreateActivity
{
    public static void MapCreateActivity(this WebApplication app)
    {
        app.MapPost("/activities", Handle).RequireAuthorization();
    }

    private static async Task<IResult> Handle(AppDbContext db,
        ClaimsPrincipal user,
        CreateActivityRequest request)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdClaim is null)
            return Results.Unauthorized();

        var userId = Guid.Parse(userIdClaim);

        var existingUser = await db.Users.FindAsync(userId);
        if (existingUser is null)
            return Results.NotFound("User not found");

        var xp = GetXpForActivity(request.Type);

        if (xp == 0)
            return Results.BadRequest("Invalid activity type");

        var activity = new Activity
        {
            UserId = userId,
            Type = request.Type,
            XpEarned = xp,
            CompletedAt = DateTime.UtcNow
        };

        existingUser.TotalXp += xp;

        db.Activities.Add(activity);
        await db.SaveChangesAsync();

        var level = LevelUtils.CalculateLevel(existingUser.TotalXp);

        return Results.Ok(new
        {
            totalXp = existingUser.TotalXp,
            currentLevel = level,
            xpEarned = xp
        });
    }

    private static int GetXpForActivity(string type)
    {
        return type switch
        {
            "LeetCodeEasy" => 10,
            "LeetCodeMedium" => 25,
            "LeetCodeHard" => 50,
            "SystemDesign" => 60,
            "ProjectRefactor" => 80,
            _ => 0
        };
    }
}