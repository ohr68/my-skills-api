using Microsoft.EntityFrameworkCore;
using MySkills.Api.Models;

namespace MySkills.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        builder.Entity<User>()
            .HasMany(u => u.Sessions)
            .WithOne(s => s.User)
            .HasForeignKey(s => s.UserId);
        
        builder.Entity<User>()
            .HasMany(u => u.Achievements)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId);
        
        builder.Entity<Activity>()
            .HasOne(a => a.User)
            .WithMany(u => u.Activities)
            .HasForeignKey(a => a.UserId);
    }
}
