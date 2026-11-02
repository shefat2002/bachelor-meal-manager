using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MealManager.Application.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MealManager.Application.Abstractions;

public interface ITokenService
{
    string CreateAccessToken(Member member, IEnumerable<string> globalRoles);

    (string Token, string Hash) NewRefreshToken();

    string HashToken(string token);
}

public sealed class TokenService(IConfiguration configuration) : ITokenService
{
    public string Issuer => configuration["Jwt:Issuer"] ?? "bmm-api";

    public string Audience => configuration["Jwt:Audience"] ?? "bmm-web";

    private SymmetricSecurityKey Key => new(Encoding.UTF8.GetBytes(
        configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.")));

    public string CreateAccessToken(Member member, IEnumerable<string> globalRoles)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, member.Id.ToString()),
            new(JwtRegisteredClaimNames.PhoneNumber, member.PhoneNumber ?? string.Empty),
            new("name", member.Name),
            new("globalRole", globalRoles.Contains("SuperAdmin") ? "superAdmin" : "user"),
        };
        claims.AddRange(globalRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(Issuer, Audience, claims, notBefore: now, expires: now.AddMinutes(15), credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string Token, string Hash) NewRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return (token, HashToken(token));
    }

    public string HashToken(string token)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
