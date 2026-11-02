using System.Text.RegularExpressions;

namespace MealManager.Application.Common;

public static partial class Phone
{
    [GeneratedRegex(@"^\+?[0-9]{8,15}$")]
    private static partial Regex Pattern();

    /// <summary>Strips separators (spaces, dashes, dots, parentheses). Result feeds validation and Identity UserName.</summary>
    public static string Normalize(string phone) =>
        new([.. phone.Where(c => c is not ' ' and not '-' and not '.' and not '(' and not ')')]);

    public static bool IsValid(string normalizedPhone) => Pattern().IsMatch(normalizedPhone);
}
