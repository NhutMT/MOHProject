using Microsoft.AspNetCore.Mvc;
using MOHProject.Application.Features.AdditionOfRider;
using MOHProject.Application.Ports;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Web.Controllers;

// Demo-only endpoints. Phase 7 will replace this with proper Razor views +
// server-side forms. Kept minimal for curl / Postman verification of the
// UAT bug flows end-to-end.
[ApiController]
[Route("policies")]
public sealed class PoliciesController : ControllerBase
{
    private readonly IPolicyRepository _policies;

    public PoliciesController(IPolicyRepository policies)
    {
        _policies = policies;
    }

    [HttpGet("{policyNumber}")]
    public async Task<IActionResult> Get(string policyNumber, CancellationToken ct)
    {
        var policy = await _policies.GetByPolicyNumberAsync(policyNumber, ct);
        if (policy is null) return NotFound(new { policyNumber, error = "Policy not found" });

        return Ok(ToDto(policy));
    }

    // POST /policies/{policyNumber}/save-uw
    // Enhancement 1 (FR-AOR-001) — auto-route to UW if any plan is Draft.
    [HttpPost("{policyNumber}/save-uw")]
    public async Task<IActionResult> SaveUw(string policyNumber, [FromBody] ActorRequest body,
        [FromServices] SaveUnderwritingTabCommandHandler handler, CancellationToken ct)
    {
        var policyId = await ResolveIdAsync(policyNumber, ct);
        if (policyId is null) return NotFound(new { policyNumber, error = "Policy not found" });

        await handler.HandleAsync(new SaveUnderwritingTabCommand(policyId.Value, body.ActorUserId), ct);
        return NoContent();
    }

    // POST /policies/{policyNumber}/resubmit-uw
    // FR-AOR-042 — only allowed from specific substatuses.
    [HttpPost("{policyNumber}/resubmit-uw")]
    public async Task<IActionResult> ResubmitUw(string policyNumber, [FromBody] ActorRequest body,
        [FromServices] ResubmitForManualUwCommandHandler handler, CancellationToken ct)
    {
        var policyId = await ResolveIdAsync(policyNumber, ct);
        if (policyId is null) return NotFound(new { policyNumber, error = "Policy not found" });

        try
        {
            await handler.HandleAsync(new ResubmitForManualUwCommand(policyId.Value, body.ActorUserId), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    // POST /policies/{policyNumber}/uw-decision
    // Body: { "decision": "Standard" | "Substandard" | "Aps" | "Declined" | "Postponed" | "NotTakenUp", "actorUserId": "..." }
    [HttpPost("{policyNumber}/uw-decision")]
    public async Task<IActionResult> UwDecision(string policyNumber, [FromBody] UwDecisionRequest body,
        [FromServices] UwDecisionCommandHandler handler, CancellationToken ct)
    {
        var policyId = await ResolveIdAsync(policyNumber, ct);
        if (policyId is null) return NotFound(new { policyNumber, error = "Policy not found" });

        if (!Enum.TryParse<UwDecision>(body.Decision, ignoreCase: true, out var decision))
            return BadRequest(new { error = $"Invalid decision '{body.Decision}'. Values: {string.Join(", ", Enum.GetNames<UwDecision>())}" });

        try
        {
            await handler.HandleAsync(new UwDecisionCommand(policyId.Value, decision, body.ActorUserId), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    // POST /policies/{policyNumber}/riders/{planId}/status
    // Body: { "newStatus": "NotTakenUp" | "Declined" | "Postponed", "actorUserId": "..." }
    [HttpPost("{policyNumber}/riders/{planId:long}/status")]
    public async Task<IActionResult> MarkRiderStatus(string policyNumber, long planId,
        [FromBody] RiderStatusRequest body,
        [FromServices] MarkRiderStatusCommandHandler handler, CancellationToken ct)
    {
        var policyId = await ResolveIdAsync(policyNumber, ct);
        if (policyId is null) return NotFound(new { policyNumber, error = "Policy not found" });

        if (!Enum.TryParse<ProductStatus>(body.NewStatus, ignoreCase: true, out var newStatus))
            return BadRequest(new { error = $"Invalid newStatus '{body.NewStatus}'." });

        try
        {
            await handler.HandleAsync(new MarkRiderStatusCommand(policyId.Value, planId, newStatus, body.ActorUserId), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    // POST /policies/{policyNumber}/riders/{planId}/re-add
    [HttpPost("{policyNumber}/riders/{planId:long}/re-add")]
    public async Task<IActionResult> ReAddRider(string policyNumber, long planId,
        [FromBody] ActorRequest body,
        [FromServices] ReAddRiderCommandHandler handler, CancellationToken ct)
    {
        var policyId = await ResolveIdAsync(policyNumber, ct);
        if (policyId is null) return NotFound(new { policyNumber, error = "Policy not found" });

        try
        {
            await handler.HandleAsync(new ReAddRiderCommand(policyId.Value, planId, body.ActorUserId), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    private async Task<long?> ResolveIdAsync(string policyNumber, CancellationToken ct)
    {
        var policy = await _policies.GetByPolicyNumberAsync(policyNumber, ct);
        return policy?.Id;
    }

    private static object ToDto(Policy p) => new
    {
        Id = p.Id,
        PolicyNumber = p.PolicyNumber,
        Type = p.Type.ToString(),
        Substatus = p.Substatus.ToString(),
        Residency = new
        {
            Insured = p.InsuredResidency.ToString(),
            Payer = p.PayerResidency.ToString(),
        },
        UwCompletedAt = p.UwCompletedAt,
        UWState = p.UWState is null ? null : new
        {
            p.UWState.RcmpFlag,
            p.UWState.RcmpFlagEnabled,
            AcceptCloa = p.UWState.AcceptCloa.ToString(),
            p.UWState.AcceptCloaEnabled,
            RcmpOption = p.UWState.RcmpOption.ToString(),
            p.UWState.RcmpOptionEnabled,
            p.UWState.CompleteUw,
            CurrentComposition = p.UWState.CurrentComposition.ToString(),
        },
        PremiumCollection = p.PremiumCollection is null ? null : new
        {
            BaseToCollect = p.PremiumCollection.BaseToCollect.ToString(),
            BaseCollected = p.PremiumCollection.BaseCollected.ToString(),
            BaseShortfall = p.PremiumCollection.BaseShortfall.ToString(),
            LinkedRidersToCollect = p.PremiumCollection.LinkedRidersToCollect.ToString(),
            LinkedRidersCollected = p.PremiumCollection.LinkedRidersCollected.ToString(),
            LinkedRidersShortfall = p.PremiumCollection.LinkedRidersShortfall.ToString(),
            TotalShortfall = p.PremiumCollection.TotalShortfall.ToString(),
            UnallocatedCash = p.PremiumCollection.UnallocatedCash.ToString(),
        },
        Plans = p.Plans.Select(plan => new
        {
            plan.Id,
            plan.ProductCode,
            plan.IsBase,
            Status = plan.Status.ToString(),
            RiskCategory = plan.RiskCategory.ToString(),
            GrossPremium = plan.GrossPremium.ToString(),
            plan.AddedAt,
            plan.StatusChangedAt,
        }),
        LetterCount = p.Letters.Count,
        AuditCount = p.AuditEntries.Count,
    };
}

public sealed record ActorRequest(string ActorUserId);
public sealed record UwDecisionRequest(string Decision, string ActorUserId);
public sealed record RiderStatusRequest(string NewStatus, string ActorUserId);
