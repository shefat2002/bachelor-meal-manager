using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Infrastructure;

public class ApplicationContext(
    DbContextOptions<ApplicationContext> options,
    ITenantContext tenant) : IdentityDbContext<Member, IdentityRole<Guid>, Guid>(options), IApplicationContext
{
    public DbSet<Mess> Messes => Set<Mess>();

    public DbSet<MemberMess> MembersMesses => Set<MemberMess>();

    public DbSet<InviteLink> InviteLinks => Set<InviteLink>();

    public DbSet<DailyMeal> DailyMeals => Set<DailyMeal>();

    public DbSet<BazarExpense> BazarExpenses => Set<BazarExpense>();

    public DbSet<UtilityType> UtilityTypes => Set<UtilityType>();

    public DbSet<UtilityPayment> UtilityPayments => Set<UtilityPayment>();

    public DbSet<Deposit> Deposits => Set<Deposit>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    DbSet<Member> IApplicationContext.Members => Users;

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Member>(e =>
        {
            e.Property(m => m.Name).IsRequired().HasMaxLength(100);
            e.Property(m => m.CreatedAt);
        });

        b.Entity<Mess>(e =>
        {
            e.Property(m => m.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(m => m.CreatedBy);
        });

        b.Entity<MemberMess>(e =>
        {
            e.HasKey(mm => new { mm.MessId, mm.MemberId });
            e.Property(mm => mm.Role).HasConversion<string>().HasMaxLength(20);
            e.Property(mm => mm.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(mm => new { mm.MessId, mm.Status });
            // Tenant isolation backbone (skill bmm-efcore-multitenancy): cross-mess reads must go through
            // IgnoreQueryFilters() in consciously-audited handlers.
            e.HasQueryFilter(mm => mm.MessId == tenant.MessId);
            e.HasOne(mm => mm.Mess)
                .WithMany(m => m.Members)
                .HasForeignKey(mm => mm.MessId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(mm => mm.Member)
                .WithMany(m => m.Messes)
                .HasForeignKey(mm => mm.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<InviteLink>(e =>
        {
            e.Property(i => i.Token).IsRequired().HasMaxLength(128);
            e.HasIndex(i => i.Token).IsUnique();
            e.HasQueryFilter(i => i.MessId == tenant.MessId);
            e.HasOne(i => i.Mess)
                .WithMany()
                .HasForeignKey(i => i.MessId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.HasIndex(t => t.Hash).IsUnique();
            e.HasIndex(t => t.ExpiresAt);
            e.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<OtpCode>(e =>
        {
            e.Property(o => o.PhoneNumber).IsRequired().HasMaxLength(20);
            e.HasIndex(o => new { o.PhoneNumber, o.CreatedAt });
        });

        b.Entity<DailyMeal>(e =>
        {
            e.Property(m => m.Breakfast).HasPrecision(4, 1);
            e.Property(m => m.Lunch).HasPrecision(4, 1);
            e.Property(m => m.Dinner).HasPrecision(4, 1);
            // Upsert key (P2-BE-1) + hot query path tenant+date scoped.
            e.HasIndex(m => new { m.MessId, m.MemberId, m.Date }).IsUnique();
            e.HasIndex(m => new { m.MessId, m.Date });
            e.HasQueryFilter(m => m.MessId == tenant.MessId);
            e.HasOne<Mess>().WithMany().HasForeignKey(m => m.MessId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Member>().WithMany().HasForeignKey(m => m.MemberId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<BazarExpense>(e =>
        {
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Quantity).HasPrecision(9, 2);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.HasIndex(x => new { x.MessId, x.Date });
            e.HasQueryFilter(x => x.MessId == tenant.MessId);
            e.HasOne<Mess>().WithMany().HasForeignKey(x => x.MessId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Member>().WithMany().HasForeignKey(x => x.BuyerMemberId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<UtilityType>(e =>
        {
            e.Property(t => t.Name).IsRequired().HasMaxLength(100);
            e.Property(t => t.Description).HasMaxLength(300);
            e.HasIndex(t => new { t.MessId, t.Name }).IsUnique();
            e.HasQueryFilter(t => t.MessId == tenant.MessId);
            e.HasOne<Mess>().WithMany().HasForeignKey(t => t.MessId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<UtilityPayment>(e =>
        {
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.Property(p => p.Note).HasMaxLength(300);
            e.HasIndex(p => new { p.MessId, p.PaymentDate });
            e.HasQueryFilter(p => p.MessId == tenant.MessId);
            e.HasOne<Mess>().WithMany().HasForeignKey(p => p.MessId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<UtilityType>().WithMany().HasForeignKey(p => p.UtilityTypeId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Deposit>(e =>
        {
            e.Property(d => d.Amount).HasPrecision(18, 2);
            e.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(d => d.Note).HasMaxLength(300);
            e.HasIndex(d => new { d.MessId, d.Date });
            e.HasQueryFilter(d => d.MessId == tenant.MessId);
            e.HasOne<Mess>().WithMany().HasForeignKey(d => d.MessId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Member>().WithMany().HasForeignKey(d => d.MemberId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
