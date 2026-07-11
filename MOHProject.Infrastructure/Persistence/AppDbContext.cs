using Microsoft.EntityFrameworkCore;
using MOHProject.Domain.Entities;
using MOHProject.Domain.ValueObjects;

namespace MOHProject.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<UWState> UWStates => Set<UWState>();
    public DbSet<PremiumCollection> PremiumCollections => Set<PremiumCollection>();
    public DbSet<Letter> Letters => Set<Letter>();
    public DbSet<LetterPlan> LetterPlans => Set<LetterPlan>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<Insured> Insureds => Set<Insured>();
    public DbSet<Payer> Payers => Set<Payer>();
    public DbSet<PolicyHolder> PolicyHolders => Set<PolicyHolder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Policy>(e =>
        {
            e.ToTable("Policies");
            e.HasKey(p => p.Id);
            e.Property(p => p.PolicyNumber).HasMaxLength(32).IsRequired();
            e.HasIndex(p => p.PolicyNumber).IsUnique();
            e.Property(p => p.RowVersion).IsRowVersion();

            e.HasOne(p => p.UWState).WithMany().HasForeignKey(p => p.UwStateId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.PremiumCollection).WithMany().HasForeignKey(p => p.PremiumCollectionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Insured).WithMany().HasForeignKey(p => p.InsuredId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Payer).WithMany().HasForeignKey(p => p.PayerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.PolicyHolder).WithMany().HasForeignKey(p => p.PolicyHolderId).OnDelete(DeleteBehavior.Restrict);

            e.HasMany(p => p.Plans).WithOne(p => p.Policy).HasForeignKey(p => p.PolicyId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Letters).WithOne(l => l.Policy).HasForeignKey(l => l.PolicyId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Reminders).WithOne(r => r.Policy).HasForeignKey(r => r.PolicyId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.AuditEntries).WithOne(a => a.Policy).HasForeignKey(a => a.PolicyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Plan>(e =>
        {
            e.ToTable("Plans");
            e.HasKey(p => p.Id);
            e.Property(p => p.ProductCode).HasMaxLength(32).IsRequired();
            e.Ignore(p => p.RiskAssessment);
            e.Ignore(p => p.RiskCategory);
            ConfigureMoney(e, p => p.GrossPremium, nameof(Plan.GrossPremium));
            ConfigureMoney(e, p => p.PrivateInsuranceExtraPremium, nameof(Plan.PrivateInsuranceExtraPremium));
        });

        modelBuilder.Entity<UWState>(e =>
        {
            e.ToTable("UWStates");
            e.HasKey(u => u.Id);
        });

        modelBuilder.Entity<PremiumCollection>(e =>
        {
            e.ToTable("PremiumCollections");
            e.HasKey(pc => pc.Id);
            e.Ignore(pc => pc.BaseShortfall);
            e.Ignore(pc => pc.LinkedRidersShortfall);
            e.Ignore(pc => pc.TotalShortfall);
            ConfigureMoney(e, pc => pc.BaseToCollect, nameof(PremiumCollection.BaseToCollect));
            ConfigureMoney(e, pc => pc.BaseCollected, nameof(PremiumCollection.BaseCollected));
            ConfigureMoney(e, pc => pc.LinkedRidersToCollect, nameof(PremiumCollection.LinkedRidersToCollect));
            ConfigureMoney(e, pc => pc.LinkedRidersCollected, nameof(PremiumCollection.LinkedRidersCollected));
            ConfigureMoney(e, pc => pc.UnallocatedCash, nameof(PremiumCollection.UnallocatedCash));
        });

        modelBuilder.Entity<Letter>(e =>
        {
            e.ToTable("Letters");
            e.HasKey(l => l.Id);
            e.Property(l => l.CorrelationId).IsRequired();
            e.HasIndex(l => new { l.PolicyId, l.Type, l.IsCurrent });
        });

        modelBuilder.Entity<LetterPlan>(e =>
        {
            e.ToTable("LetterPlans");
            e.HasKey(lp => new { lp.LetterId, lp.PlanId });
            e.HasOne(lp => lp.Letter).WithMany(l => l.IncludedPlans).HasForeignKey(lp => lp.LetterId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(lp => lp.Plan).WithMany().HasForeignKey(lp => lp.PlanId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Reminder>(e =>
        {
            e.ToTable("Reminders");
            e.HasKey(r => r.Id);
            e.HasOne(r => r.ParentLetter).WithMany().HasForeignKey(r => r.ParentLetterId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(r => new { r.PolicyId, r.Status });
        });

        modelBuilder.Entity<AuditEntry>(e =>
        {
            e.ToTable("AuditEntries");
            e.HasKey(a => a.Id);
            e.Property(a => a.ActorUserId).HasMaxLength(128).IsRequired();
            e.Property(a => a.EventType).HasMaxLength(64).IsRequired();
            e.Property(a => a.PayloadJson).HasColumnType("nvarchar(max)");
            e.HasIndex(a => new { a.PolicyId, a.OccurredAt });
        });

        modelBuilder.Entity<Insured>(e =>
        {
            e.ToTable("Insureds");
            e.HasKey(i => i.Id);
            e.Property(i => i.ExternalId).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<Payer>(e =>
        {
            e.ToTable("Payers");
            e.HasKey(p => p.Id);
            e.Property(p => p.ExternalId).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<PolicyHolder>(e =>
        {
            e.ToTable("PolicyHolders");
            e.HasKey(h => h.Id);
            e.Property(h => h.ExternalId).HasMaxLength(64).IsRequired();
        });
    }

    private static void ConfigureMoney<T>(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> builder,
        System.Linq.Expressions.Expression<Func<T, Money>> selector,
        string columnPrefix) where T : class
    {
        builder.ComplexProperty(selector, m =>
        {
            m.Property(x => x.Amount).HasColumnType("decimal(18,2)").HasColumnName($"{columnPrefix}_Amount");
            m.Property(x => x.Currency).HasMaxLength(3).HasColumnName($"{columnPrefix}_Currency");
        });
    }
}
