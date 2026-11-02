namespace MealManager.Application.Domain;

/// <summary>Rotating refresh token; only the SHA-256 hash is stored.</summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Hash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByHash { get; set; }

    public Member User { get; set; } = null!;
}
