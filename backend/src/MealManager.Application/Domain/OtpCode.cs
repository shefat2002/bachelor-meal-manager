namespace MealManager.Application.Domain;

/// <summary>Login OTP for an existing phone-number account; code is stored hashed, single-use, 5-minute expiry.</summary>
public class OtpCode
{
    public Guid Id { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public string CodeHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public int Attempts { get; set; }

    public DateTime? ConsumedAt { get; set; }
}
