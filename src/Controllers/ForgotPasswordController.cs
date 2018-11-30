using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Schematic.Core;
using Schematic.Identity;

namespace Schematic.Core.Mvc
{
    public class ForgotPasswordController : Controller
    {
        protected readonly ISchematicSettings Settings;
        protected readonly IEmailValidator EmailValidator;
        protected readonly IEmailSender EmailSender;
        protected readonly IForgotPasswordEmail<User> ForgotPasswordEmail;
        protected readonly IUserRepository<User, UserFilter> UserRepository;
        protected readonly IStringLocalizer<ForgotPasswordViewModel> Localizer;
        
        protected User AuthenticationUser;

        public ForgotPasswordController(
            ISchematicSettings settings,
            IEmailValidator emailValidator,
            IEmailSender emailSender,
            IForgotPasswordEmail<User> forgotPasswordEmail,
            IUserRepository<User, UserFilter> userRepository,
            IStringLocalizer<ForgotPasswordViewModel> localizer)
        {
            Settings = settings;
            EmailValidator = emailValidator;
            EmailSender = emailSender;
            ForgotPasswordEmail = forgotPasswordEmail;
            UserRepository = userRepository;
            Localizer = localizer;
        }

        [BindProperty]
        public ForgotPasswordViewModel ForgotPasswordData { get; set; }

        [Route("{culture}/in/forgot")]
        [HttpGet]
        public IActionResult Authentication()
        {
            var data = new AuthenticationViewModel();
            data.Mode = "forgot-password";

            return View("/Views/Authentication/Authentication.cshtml", data);
        }

        [Route("{culture}/in/forgot-password")]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            var data = new ForgotPasswordViewModel();
            return PartialView(data);
        }

        [Route("{culture}/in/forgot-password")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel data)
        {
            ViewData["Email"] = data.Email;
            
            if (!ModelState.IsValid)
            {
                return PartialView(data);
            }

            if (!EmailValidator.IsValidEmail(data.Email))
            {
                ModelState.AddModelError("InvalidEmail", Localizer[AuthenticationErrorMessages.InvalidEmail]);
                return PartialView(data);
            }

            AuthenticationUser = UserRepository.ReadByEmail(data.Email);

            if (AuthenticationUser == null)
            {
                ModelState.AddModelError("UserDoesNotExist", Localizer[AuthenticationErrorMessages.UserDoesNotExist]);
                return PartialView(data);
            }

            string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var saveToken = UserRepository.SaveToken(AuthenticationUser, token);

            if (!saveToken)
            {
                ModelState.AddModelError("ReminderFailed", Localizer[AuthenticationErrorMessages.PasswordReminderFailed]);
                return PartialView(data);
            }

            var domain = Request.Host.Value;
            domain += (Request.PathBase.Value.HasValue()) ? "/" + Request.PathBase.Value : string.Empty;
            var emailSubject = ForgotPasswordEmail.Subject();
            var emailBody = ForgotPasswordEmail.Body(AuthenticationUser, domain, emailSubject, token);

            await EmailSender.SendEmailAsync(data.Email, emailSubject, emailBody);

            data.SendReminderSuccess = true;

            return PartialView(data);
        }
    }
}