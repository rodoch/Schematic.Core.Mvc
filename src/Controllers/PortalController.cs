using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Schematic.Core.Mvc
{
    [Route("{culture}/portal")]
    [Authorize]
    public class PortalController : Controller
    {
        IStringLocalizer<SharedResource> Localizer;

        public PortalController(IStringLocalizer<SharedResource> localizer)
        {
            Localizer = localizer;
        }

        [HttpGet]
        public IActionResult Portal()
        {
            ViewData["PageTitle"] = Localizer["Portal"];

            return View();
        }
    }
}