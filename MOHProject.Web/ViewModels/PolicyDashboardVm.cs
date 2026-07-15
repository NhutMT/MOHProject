using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Web.ViewModels;

// PH-07-09 — display-shape projection of the Policy aggregate.
// No domain entity leakage into Razor: view binds only to this VM.
public sealed record PolicyDashboardVm
{
    public required string PolicyNumber { get; init; }
    public required string Type { get; init; }
    public required string Substatus { get; init; }
    public required string SubstatusBadgeClass { get; init; }
    public string? UwCompletedAt { get; init; }

    public required string InsuredResidency { get; init; }
    public required string PayerResidency { get; init; }

    public UwStateVm? UwState { get; init; }
    public PremiumSummaryVm? PremiumSummary { get; init; }
    public required IReadOnlyList<PlanRowVm> Plans { get; init; }
    public required IReadOnlyList<LetterRowVm> Letters { get; init; }
    public required IReadOnlyList<AuditRowVm> RecentAudit { get; init; }

    public static PolicyDashboardVm From(Policy p) => new()
    {
        PolicyNumber = p.PolicyNumber,
        Type = p.Type.ToString(),
        Substatus = FormatSubstatus(p.Substatus),
        SubstatusBadgeClass = SubstatusColor(p.Substatus),
        UwCompletedAt = p.UwCompletedAt?.ToString("yyyy-MM-dd HH:mm 'UTC'"),
        InsuredResidency = p.InsuredResidency.ToString(),
        PayerResidency = p.PayerResidency.ToString(),
        UwState = p.UWState is null ? null : UwStateVm.From(p.UWState),
        PremiumSummary = p.PremiumCollection is null ? null : PremiumSummaryVm.From(p.PremiumCollection),
        Plans = p.Plans
            .OrderByDescending(pl => pl.IsBase)
            .ThenBy(pl => pl.ProductCode, StringComparer.Ordinal)
            .Select(PlanRowVm.From)
            .ToList(),
        Letters = p.Letters
            .OrderByDescending(l => l.IssuedAt)
            .Select(LetterRowVm.From)
            .ToList(),
        RecentAudit = p.AuditEntries
            .OrderByDescending(a => a.OccurredAt)
            .Take(10)
            .Select(AuditRowVm.From)
            .ToList(),
    };

    private static string FormatSubstatus(PolicySubstatus s) =>
        System.Text.RegularExpressions.Regex.Replace(s.ToString(), "([A-Z])", " $1").Trim();

    private static string SubstatusColor(PolicySubstatus s) => s switch
    {
        PolicySubstatus.PolicyIncepted => "badge-success",
        PolicySubstatus.PendingCashCollection => "badge-warning",
        PolicySubstatus.ConditionalAcceptanceLetterGenerated => "badge-info",
        PolicySubstatus.PendingUwAps => "badge-danger",
        _ => "badge-secondary",
    };
}

public sealed record UwStateVm(bool RcmpFlag, bool RcmpFlagEnabled,
                               string AcceptCloa, bool AcceptCloaEnabled,
                               string RcmpOption, bool RcmpOptionEnabled,
                               bool CompleteUw, string CurrentComposition)
{
    public static UwStateVm From(UWState s) => new(
        s.RcmpFlag, s.RcmpFlagEnabled,
        s.AcceptCloa.ToString(), s.AcceptCloaEnabled,
        s.RcmpOption.ToString(), s.RcmpOptionEnabled,
        s.CompleteUw, s.CurrentComposition.ToString());
}

public sealed record PremiumSummaryVm(
    string BaseToCollect, string BaseCollected, string BaseShortfall,
    string LinkedRidersToCollect, string LinkedRidersCollected, string LinkedRidersShortfall,
    string TotalShortfall, string UnallocatedCash,
    bool HasShortfall)
{
    public static PremiumSummaryVm From(PremiumCollection pc) => new(
        pc.BaseToCollect.ToString(),
        pc.BaseCollected.ToString(),
        pc.BaseShortfall.ToString(),
        pc.LinkedRidersToCollect.ToString(),
        pc.LinkedRidersCollected.ToString(),
        pc.LinkedRidersShortfall.ToString(),
        pc.TotalShortfall.ToString(),
        pc.UnallocatedCash.ToString(),
        HasShortfall: pc.TotalShortfall.IsPositive);
}

public sealed record PlanRowVm(long Id, string ProductCode, bool IsBase,
                               string Status, string RiskCategory, string GrossPremium,
                               string? AddedAt, string? StatusChangedAt)
{
    public static PlanRowVm From(Plan p) => new(
        p.Id, p.ProductCode, p.IsBase,
        p.Status.ToString(), p.RiskCategory.ToString(),
        p.GrossPremium.ToString(),
        p.AddedAt?.ToString("yyyy-MM-dd"),
        p.StatusChangedAt?.ToString("yyyy-MM-dd HH:mm 'UTC'"));
}

public sealed record LetterRowVm(long Id, string Type, string IssuedAt, bool IsCurrent, string CorrelationId)
{
    public static LetterRowVm From(Letter l) => new(
        l.Id, l.Type.ToString(),
        l.IssuedAt.ToString("yyyy-MM-dd HH:mm 'UTC'"),
        l.IsCurrent, l.CorrelationId.ToString("N")[..8]);
}

public sealed record AuditRowVm(string OccurredAt, string ActorUserId, string EventType)
{
    public static AuditRowVm From(AuditEntry a) => new(
        a.OccurredAt.ToString("yyyy-MM-dd HH:mm 'UTC'"),
        a.ActorUserId,
        a.EventType);
}
