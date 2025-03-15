using System;

public static class OptionAliasGenerator
{
    public static string[] GenerateAliases(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be null/empty.", nameof(name));

        // Esempio "light": la forma lunga e la short letter
        string lower = name.ToLowerInvariant();
        string shortLetter = lower[0].ToString();
        return new[]
        {
            $"--{lower}",
            $"-{shortLetter}"
        };
    }
}