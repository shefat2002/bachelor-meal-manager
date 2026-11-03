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

    DbSet<DailyMeal> DailyMeals { get; }

    DbSet<BazarExpense> BazarExpenses { get; }

    DbSet<UtilityType> UtilityTypes { get; }

    DbSet<UtilityPayment> UtilityPayments { get; }

    DbSet<Deposit> Deposits { get; }

    DbSet<MonthLock> MonthLocks { get; }

    DbSet<Domain.Notification> Notifications { get; }

    DbSet<ReportJob> ReportJobs { get; }

    DbSet<ReportFile> ReportFiles { get; }

    DbSet<ProcessedMessage> ProcessedMessages { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<OtpCode> OtpCodes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
