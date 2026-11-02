using MealManager.Application.Domain;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Abstractions;

/// <summary>Persistence contract consumed by handlers; implemented by ApplicationContext in Infrastructure.</summary>
public interface IApplicationContext
{
    DbSet<Member> Members { get; }

    DbSet<Mess> Messes { get; }

    DbSet<MemberMess> MembersMesses { get; }

    DbSet<InviteLink> InviteLinks { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<OtpCode> OtpCodes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
