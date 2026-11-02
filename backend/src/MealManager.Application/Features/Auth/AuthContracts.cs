namespace MealManager.Application.Features.Auth;

public record AuthUserDto(Guid Id, string Name, string Phone, string GlobalRole);

/// <summary>Issued by register/login/otp-verify/refresh. The refresh token travels server-side only —
/// controllers put it in the HttpOnly cookie and strip it from the JSON response.</summary>
public record AuthResult(string AccessToken, string RefreshToken, AuthUserDto User);

public record OtpRequestResult(bool Sent, string? DevCode, DateTime ExpiresAt);

public static class AuthRoles
{
    public const string SuperAdmin = "SuperAdmin";
}
