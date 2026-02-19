namespace MySkills.Api.Utils;

public static class LevelUtils
{
    public static int CalculateLevel(int totalXp) => totalXp / 100;

    public static int CalculateXpToNextLevel(int totalXp) => 100 - (totalXp % 100);
}