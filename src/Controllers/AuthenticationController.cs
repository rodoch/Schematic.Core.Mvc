using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Schematic.Core;
using Schematic.Identity;

namespace Schematic.Core.Mvc
{
    public class AuthenticationController : Controller
    {
        protected readonly IPasswordHasherService<User> PasswordHasherService;
        protected readonly IUserRepository<User, UserFilter, UserSpecification> UserRepository;
        protected readonly IStringLocalizer<SignInViewModel> Localizer;

        protected User AuthenticationUser;

        public AuthenticationController(
            IPasswordHasherService<User> passwordHasherService,
            IUserRepository<User, UserFilter, UserSpecification> userRepository,
            IStringLocalizer<SignInViewModel> localizer)
        {
            PasswordHasherService = passwordHasherService;
            UserRepository = userRepository;
            Localizer = localizer;
        }

        [BindProperty]
        public SignInViewModel SignInData { get; set; }
        
        protected bool IsValidUser { get; set; } = true;

        [Route("{culture?}/{in?}")]
        [HttpGet]
        public IActionResult Authentication()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToRoute("default");
            }

            var data = new AuthenticationViewModel();
            data.Mode = "sign-in";

            return View(data);
        }

        [Route("{culture}/in/sign-in")]
        [HttpGet]
        public IActionResult SignIn()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToRoute("default");
            }

            return PartialView();
        }

        [Route("{culture}/in/sign-in")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(SignInViewModel data)
        {
            ViewData["Email"] = data.Email;
            
            if (!ModelState.IsValid)
            {
                return PartialView();
            }

            var userSpecification = new UserSpecification()
            {
                Email = data.Email
            };

            AuthenticationUser = await UserRepository.ReadAsync(userSpecification);

            if (AuthenticationUser == null)
            {
                ModelState.AddModelError("Invalid", 
                    Localizer[AuthenticationErrorMessages.InvalidData]);
                return PartialView();
            }

            var passwordVerification = PasswordHasherService.VerifyHashedPassword(AuthenticationUser, 
                AuthenticationUser.PassHash, SignInData.Password);

            if (passwordVerification == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("Invalid", 
                    Localizer[AuthenticationErrorMessages.InvalidData]);
                return PartialView();
            }

            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, 
                ClaimTypes.Name, ClaimTypes.Role);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, AuthenticationUser.ID.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Name, AuthenticationUser.FullName));
            identity.AddClaim(new Claim(ClaimTypes.Email, AuthenticationUser.Email));

            foreach (var role in AuthenticationUser.Roles.Where(r => r.HasRole))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role.Name));
            }

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, 
                new AuthenticationProperties { IsPersistent = data.RememberMe });

            return Json(new { Route = Url.RouteUrl("default") });
        }

        [Route("{culture}/out")]
        [HttpPost]
        public async Task<IActionResult> SignOut() 
        { 
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Authentication");
        }
    }
}