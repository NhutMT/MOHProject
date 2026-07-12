using Microsoft.AspNetCore.Mvc;
using MOHProject.Application.Ports;
using MOHProject.Domain.Entities;

namespace MOHProject.Web.Controllers;

// Demo-only endpoint. Phase 7 will replace this with proper Razor views.
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
