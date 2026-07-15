using Microsoft.AspNetCore.Mvc;
using MOHProject.Infrastructure.Persistence;

namespace MOHProject.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() =>
        Content($$"""
            MOH SHIELD scaffold

            Demo policy: {{DemoSeeder.DemoPolicyNumber}}

            - HTML dashboard: /pages/policies/{{DemoSeeder.DemoPolicyNumber}}/dashboard
            - JSON API:       /policies/{{DemoSeeder.DemoPolicyNumber}}

            POST endpoints (demo, no auth):
              POST /policies/{policyNumber}/save-uw
              POST /policies/{policyNumber}/resubmit-uw
              POST /policies/{policyNumber}/uw-decision           body: { decision, actorUserId }
              POST /policies/{policyNumber}/riders/{planId}/status  body: { newStatus, actorUserId }
              POST /policies/{policyNumber}/riders/{planId}/re-add  body: { actorUserId }
            """);
}
