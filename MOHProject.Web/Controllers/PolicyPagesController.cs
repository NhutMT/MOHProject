using Microsoft.AspNetCore.Mvc;
using MOHProject.Application.Ports;
using MOHProject.Web.ViewModels;

namespace MOHProject.Web.Controllers;

// PH-07-01 — Razor-driven Policy Dashboard. Separate controller from the
// API-shaped PoliciesController so both surfaces can coexist during the
// UI rollout: JSON at /policies/*, HTML at /pages/policies/*.
[Route("pages/policies")]
public sealed class PolicyPagesController : Controller
{
    private readonly IPolicyRepository _policies;

    public PolicyPagesController(IPolicyRepository policies)
    {
        _policies = policies;
    }

    [HttpGet("{policyNumber}/dashboard")]
    public async Task<IActionResult> Dashboard(string policyNumber, CancellationToken ct)
    {
        var policy = await _policies.GetByPolicyNumberAsync(policyNumber, ct);
        if (policy is null) return NotFound(new { policyNumber, error = "Policy not found" });

        var vm = PolicyDashboardVm.From(policy);
        return View(vm);
    }
}
