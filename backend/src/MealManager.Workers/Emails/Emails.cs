using MealManager.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace MealManager.Workers.Emails;

public sealed class ResendOptions
{
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Verified-domain sender, e.g. "Bachelor Meal Manager &lt;noreply@meal-manager.bro.al&gt;".</summary>
    public string From { get; set; } = string.Empty;
}

/// <summary>
/// Resend over a typed HttpClient (POST /emails, Bearer key — api.resend.com). Best-effort:
/// rejections (4xx) and transient failures are logged, never thrown — email must not fail a
/// message batch the way a poisoned handler would.
/// </summary>
public sealed class ResendEmailSender(
    HttpClient http,
    IOptions<ResendOptions> options,
    ILogger<ResendEmailSender> logger) : IEmailSender
{
    private sealed record SendRequest(string From, string[] To, string Subject, string Html);

    public async Task SendAsync(string to, string subject, string html, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await http.PostAsJsonAsync(
                "/emails",
                new SendRequest(options.Value.From, [to], subject, html),
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Resend rejected email to {RecipientHash}: {Status} {Body}",
                    Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(to)))[..8],
                    (int)response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Resend delivery to {RecipientHash} failed (best-effort, skipped)",
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(to)))[..8]);
        }
    }
}

/// <summary>Local dev without an API key: in-app notifications still work, emails are logged only.</summary>
public sealed class NullEmailSender(ILogger<NullEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string html, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email skipped (no Resend key configured): {Subject} → {RecipientHash}",
            subject, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(to)))[..8]);
        return Task.CompletedTask;
    }
}

/// <summary>Plain inline-HTML templates — no engine needed at this volume.</summary>
public static class EmailTemplates
{
    private static string Shell(string heading, string bodyHtml) => $"""
        <!doctype html><html><body style="font-family:system-ui,Segoe UI,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#0f172a">
          <h2 style="margin:0 0 16px">{heading}</h2>
          {bodyHtml}
          <p style="margin-top:32px;font-size:12px;color:#64748b">Sent by Bachelor Meal Manager.</p>
        </body></html>
        """;

    public static string Invitation(string messName, string inviteUrl, DateTime expiresAtUtc) => Shell(
        $"You're invited to join {messName}",
        $"""
        <p>The manager of <b>{messName}</b> invited you to manage meals, expenses and settlements together.</p>
        <p><a href="{inviteUrl}" style="display:inline-block;background:#4f46e5;color:#fff;padding:10px 18px;border-radius:8px;text-decoration:none">Join {messName}</a></p>
        <p style="font-size:13px;color:#475569">This single-use link expires {expiresAtUtc:yyyy-MM-dd HH:mm} UTC.</p>
        """);

    public static string MembershipDecision(string messName, bool approved) => Shell(
        approved ? $"Welcome to {messName}!" : $"Your request to join {messName} was declined",
        approved
            ? $"<p>Your join request for <b>{messName}</b> was approved — you're in. Open the app to start logging meals.</p>"
            : $"<p>The manager of <b>{messName}</b> declined your join request. You can request again with a new invite.</p>");

    public static string RoleChanged(string messName, string role) => Shell(
        $"Your role in {messName} changed",
        $"<p>You are now a <b>{role}</b> of <b>{messName}</b>.</p>");
}
