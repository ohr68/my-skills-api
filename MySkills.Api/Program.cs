using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MySkills.Api.Data;
using MySkills.Api.Features.Activities;
using MySkills.Api.Features.Dashboard;
using MySkills.Api.Features.Sessions;
using MySkills.Api.Interfaces;
using MySkills.Api.Models;
using MySkills.Api.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAchievementService, AchievementService>();
builder.Services.AddScoped<IStreakService, StreakService>();

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

app.MapCreateActivity();
app.MapCreateSession();
app.MapGetDashboard();

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