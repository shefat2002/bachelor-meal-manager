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
    }
}
