using MySkills.Api.Models;

namespace MySkills.Api.Interfaces;

public interface IAchievementService
{
    List<Achievement> Evaluate(User user);
}
