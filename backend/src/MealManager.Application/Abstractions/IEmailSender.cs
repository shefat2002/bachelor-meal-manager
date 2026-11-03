namespace MealManager.Application.Abstractions;

/// <summary>
/// Transactional email port. Production: Resend (typed HttpClient over https://api.resend.com).
/// Unconfigured (local dev without a key): NullEmailSender logs. Tests: in-memory recorder.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string html, CancellationToken cancellationToken = default);
}
