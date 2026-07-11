using Microsoft.AspNetCore.Mvc;

namespace MOHProject.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => Content("MOH SHIELD scaffold");
}
