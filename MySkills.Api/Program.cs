using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MySkills.Api.Data;
using MySkills.Api.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=devlevel.db"));

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policyBuilder =>
    {
        policyBuilder
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

const string jwtKey = "THIS_IS_SUPER_SECRET_KEY_CHANGE_LATER_123456";

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => { options.Title = "Dev Level Tracker API"; });
}

app.MapGet("/", () => "Dev Level Tracker API Running");


// =========================
// REGISTER
// =========================

app.MapPost("/register", async (AppDbContext db, RegisterRequest request) =>
{
    if (await db.Users.AnyAsync(u => u.Email == request.Email))
        return Results.BadRequest("Email already registered");

    var user = new User
    {
        Name = request.Name,
        Email = request.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok();
});


// =========================
// LOGIN
// =========================

app.MapPost("/login", async (AppDbContext db, LoginRequest request) =>
{
    var user = await db.Users
        .Include(u => u.Achievements)
        .Include(u => u.Sessions)
        .FirstOrDefaultAsync(u => u.Email == request.Email);

    if (user is null)
        return Results.BadRequest("Invalid credentials");

    var isValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

    if (!isValid)
        return Results.BadRequest("Invalid credentials");

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Name!),
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.UtcNow.AddHours(3),
        signingCredentials: credentials);

    var jwt = new JwtSecurityTokenHandler().WriteToken(token);

    var achievements = new List<Achievement>();

    bool HasAchievement(string code) =>
        user.Achievements!.Any(a => a.Code == code);

// First Session
    if (!user.Sessions!.Any() && !HasAchievement("FIRST_SESSION"))
    {
        achievements.Add(new Achievement
        {
            Id = Guid.NewGuid(),
            Code = "FIRST_SESSION",
            Title = "First Step 🚀",
            UnlockedAt = DateTime.UtcNow,
            UserId = user.Id,
        });
    }

// Streak 3
    if (user.CurrentStreak >= 3 && !HasAchievement("STREAK_3"))
    {
        achievements.Add(new Achievement
        {
            Id = Guid.NewGuid(),
            Code = "STREAK_3",
            Title = "On Fire 🔥",
            UnlockedAt = DateTime.UtcNow,
            UserId = user.Id,
        });
    }

// Streak 7
    if (user.CurrentStreak >= 7 && !HasAchievement("STREAK_7"))
    {
        achievements.Add(new Achievement
        {
            Id = Guid.NewGuid(),
            Code = "STREAK_7",
            Title = "Consistency Master 💪",
            UnlockedAt = DateTime.UtcNow,
            UserId = user.Id,
        });
    }

// XP Milestones
    if (user.TotalXp >= 100 && !HasAchievement("XP_100"))
    {
        achievements.Add(new Achievement
        {
            Id = Guid.NewGuid(),
            Code = "XP_100",
            Title = "100 XP Achieved 🎯",
            UnlockedAt = DateTime.UtcNow,
            UserId = user.Id,
        });
    }

    if (user.TotalXp >= 500 && !HasAchievement("XP_500"))
    {
        achievements.Add(new Achievement
        {
            Id = Guid.NewGuid(),
            Code = "XP_500",
            Title = "500 XP Elite 🏆",
            UnlockedAt = DateTime.UtcNow,
            UserId = user.Id,
        });
    }

    db.Achievements.AddRange(achievements);

    return Results.Ok(new { token = jwt });
});


// =========================
// PROTECTED TEST ENDPOINT
// =========================

app.MapGet("/me", (ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var name = user.FindFirstValue(ClaimTypes.Name);

    return Results.Ok(new
    {
        userId,
        name,
    });
}).RequireAuthorization();

app.MapPost("/activities", async (
    AppDbContext db,
    ClaimsPrincipal user,
    CreateActivityRequest request) =>
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

    var level = CalculateLevel(existingUser.TotalXp);

    return Results.Ok(new
    {
        totalXp = existingUser.TotalXp,
        currentLevel = level,
        xpEarned = xp
    });
}).RequireAuthorization();

app.MapGet("/dashboard", async (
    AppDbContext db,
    ClaimsPrincipal user) =>
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

    var level = CalculateLevel(existingUser.TotalXp);
    var xpToNextLevel = CalculateXpToNextLevel(existingUser.TotalXp);

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
}).RequireAuthorization();

app.MapPost("/users/{userId:guid}/sessions", async (
    Guid userId,
    CreateSessionDto dto,
    AppDbContext db) =>
{
    var user = await db.Users.FindAsync(userId);
    if (user is null)
        return Results.NotFound();

    var session = new Session
    {
        Id = Guid.NewGuid(),
        Title = dto.Title,
        Type = dto.Type,
        Difficulty = dto.Difficulty,
        XpEarned = dto.XpEarned,
        Date = DateOnly.FromDateTime(DateTime.UtcNow),
        UserId = userId
    };

    user.TotalXp += dto.XpEarned;

    var today = DateOnly.FromDateTime(DateTime.UtcNow);

    if (user.LastActivityDate is null)
    {
        user.CurrentStreak = 1;
    }
    else
    {
        var difference = today.DayNumber - user.LastActivityDate.Value.DayNumber;

        if (difference == 0)
        {
            // already logged today — do nothing
        }
        else if (difference == 1)
        {
            user.CurrentStreak += 1;
        }
        else
        {
            user.CurrentStreak = 1;
        }
    }

    if (user.CurrentStreak > user.LongestStreak)
        user.LongestStreak = user.CurrentStreak;

    user.LastActivityDate = today;

    db.Sessions.Add(session);
    await db.SaveChangesAsync();

    return Results.Ok(session);
});


static int GetXpForActivity(string type)
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

static int CalculateLevel(int totalXp)
{
    return totalXp / 100;
}

static int CalculateXpToNextLevel(int totalXp)
{
    return 100 - (totalXp % 100);
}

app.UseCors("CorsPolicy");

app.Run();


// =========================
// REQUEST DTOs
// =========================

record RegisterRequest(string Name, string Email, string Password);

record LoginRequest(string Email, string Password);

record CreateActivityRequest(string Type);

record CreateSessionDto(
    string Title,
    string Type,
    string Difficulty,
    int XpEarned
);

record AchievementDto(
    string Code,
    string Title,
    DateTime UnlockedAt
);