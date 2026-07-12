using Microsoft.EntityFrameworkCore;
using MOHProject.Application.Ports;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Infrastructure.Persistence;

// Implements PH-04-02 (supersession + correlation) plus PH-04-04..-06
// (all letter types have a generator).
//
// Behavior:
//   1. Load policy + plans.
//   2. Mark any previously-current letter OF THE SAME TYPE on this policy
//      as IsCurrent = false. Different types don't interfere (superseding
//      an LOA does not touch a CloaExclusion).
//   3. Pick which plans this letter covers based on type (FR-LTR-005 +
//      PH-04-07 + PH-04-08 newly-added filter for NTU/Decline/Postpone).
//   4. Insert the new Letter row with a fresh CorrelationId, IsCurrent=true.
public sealed class EfLetterGenerator : ILetterGenerator
{
    private readonly AppDbContext _db;

    public EfLetterGenerator(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Letter> GenerateAsync(long policyId, LetterType type, CancellationToken ct)
    {
        var policy = await _db.Policies
            .Include(p => p.Plans)
            .Include(p => p.Letters)
            .FirstOrDefaultAsync(p => p.Id == policyId, ct)
            ?? throw new InvalidOperationException($"Policy {policyId} not found.");

        // Supersede any current letter of the same type on this policy.
        foreach (var previous in policy.Letters.Where(l => l.Type == type && l.IsCurrent))
            previous.IsCurrent = false;

        var included = SelectIncludedPlans(policy, type).ToList();

        var letter = new Letter
        {
            PolicyId = policyId,
            Type = type,
            IssuedAt = DateTime.UtcNow,
            IsCurrent = true,
            CorrelationId = Guid.NewGuid(),
        };

        foreach (var plan in included)
            letter.IncludedPlans.Add(new LetterPlan { PlanId = plan.Id });

        _db.Letters.Add(letter);
        await _db.SaveChangesAsync(ct);
        return letter;
    }

    private static IEnumerable<Plan> SelectIncludedPlans(Policy policy, LetterType type) => type switch
    {
        // Main letters + auxiliary informational letters cover Active plans only
        // (FR-LTR-005: exclude Declined / Postponed / NTU / Terminated / Cancelled).
        LetterType.Loa or
        LetterType.CloaExclusion or
        LetterType.CloaRcmp or
        LetterType.MedicalEvidence or
        LetterType.RefundOfExcessPremium or
        LetterType.PremiumNotification
            => policy.Plans.Where(p => p.Status == ProductStatus.Active),

        // Rider-status letters cover plans whose status changed in the current
        // UW cycle. Riders NTU'd/Declined/Postponed before the current cycle
        // are excluded (PH-04-08 · source lines 692-712).
        LetterType.NtuWithoutRefund or LetterType.NtuWithRefund
            => policy.Plans.Where(p => p.Status == ProductStatus.NotTakenUp && IsInCurrentCycle(p, policy)),

        LetterType.Decline or LetterType.DeclineWithRefund
            => policy.Plans.Where(p => p.Status == ProductStatus.Declined && IsInCurrentCycle(p, policy)),

        LetterType.Postponement or LetterType.PostponementWithRefund
            => policy.Plans.Where(p => p.Status == ProductStatus.Postponed && IsInCurrentCycle(p, policy)),

        // Reminders reference the parent letter, not plans directly (Phase 5).
        LetterType.LoaReminder or
        LetterType.LoaFinalReminder or
        LetterType.CloaReminder or
        LetterType.CloaFinalReminder
            => Enumerable.Empty<Plan>(),

        _ => throw new InvalidOperationException($"Unhandled letter type: {type}"),
    };

    private static bool IsInCurrentCycle(Plan plan, Policy policy)
    {
        // If no UW cycle has completed yet, every change counts as "current".
        if (policy.UwCompletedAt is null) return true;

        // A rider whose status transition happened AT OR AFTER the last UW
        // completion is considered "in the current cycle". Riders whose
        // transition predates that timestamp were handled in a prior cycle
        // and must not be re-included per PH-04-08.
        return plan.StatusChangedAt is null || plan.StatusChangedAt >= policy.UwCompletedAt;
    }
}
