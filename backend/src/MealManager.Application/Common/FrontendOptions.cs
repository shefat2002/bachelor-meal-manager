namespace MealManager.Application.Common;

/// <summary>
/// Frontend base URL — used to build absolute links inside async emails/notifications
/// (e.g. invite links). Bound from the "App" config section; default covers local dev.
/// </summary>
public sealed class FrontendOptions
{
    public string Url { get; set; } = "http://localhost:3000";
}
