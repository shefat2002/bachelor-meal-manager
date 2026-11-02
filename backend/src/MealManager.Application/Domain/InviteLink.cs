namespace MealManager.Application.Domain;

public class InviteLink
{
    public Guid Id { get; set; }

    public Guid MessId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public bool Used { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public Mess Mess { get; set; } = null!;
}
