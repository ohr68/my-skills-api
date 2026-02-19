using MySkills.Api.Interfaces;
using MySkills.Api.Models;

namespace MySkills.Api.Services;

public class StreakService : IStreakService
{
    public void Update(User user, DateOnly activityDate)
    {
        if (user.LastActivityDate is null)
        {
            user.CurrentStreak = 1;
            user.LongestStreak = 1;
            user.LastActivityDate = activityDate;
            return;
        }

        var difference = activityDate.DayNumber - user.LastActivityDate.Value.DayNumber;

        switch (difference)
        {
            case 0:
                return;
            case 1:
                user.CurrentStreak += 1;
                break;
            default:
                user.CurrentStreak = 1;
                break;
        }

        if (user.CurrentStreak > user.LongestStreak)
        {
            user.LongestStreak = user.CurrentStreak;
        }

        user.LastActivityDate = activityDate;
    }
}
