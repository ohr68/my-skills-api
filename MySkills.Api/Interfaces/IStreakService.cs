using MySkills.Api.Models;

namespace MySkills.Api.Interfaces;

public interface IStreakService
{
    void Update(User user, DateOnly activityDate);
}
