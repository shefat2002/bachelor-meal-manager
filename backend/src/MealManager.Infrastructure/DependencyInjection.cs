using System.Text;
using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MealManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationContext>(options =>
            options.UseNpgsql(
                ConvertToNpgsqlConnectionString(configuration.GetConnectionString("messdb")),
                npgsql => npgsql.EnableRetryOnFailure()));
        services.AddScoped<IApplicationContext>(sp => sp.GetRequiredService<ApplicationContext>());

        services
            .AddIdentityCore<Member>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = false;
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddSingleton<ITokenService, TokenService>();
        return services;
    }

    /// <summary>
    /// Npgsql does not accept libpq URI connection strings (postgresql://user:pass@host/db?...) — it needs
    /// keyword format. Neon env vars are URIs (with libpq-only params like channel_binding), so convert them;
    /// already-keyword strings pass through unchanged.
    /// </summary>
    public static string? ConvertToNpgsqlConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString)
            || (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                && !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new StringBuilder()
            .Append("Host=").Append(uri.Host)
            .Append(";Port=").Append(uri.Port)
            .Append(";Username=").Append(Uri.UnescapeDataString(userInfo[0]));
        if (userInfo.Length == 2)
        {
            builder.Append(";Password=").Append(Uri.UnescapeDataString(userInfo[1]));
        }

        builder.Append(";Database=").Append(uri.AbsolutePath.Trim('/'));
        builder.Append(";SSL Mode=Require"); // Neon requires TLS
        return builder.ToString();
    }
}
