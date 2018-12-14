using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Schematic.Core;
using Schematic.Identity;

namespace Schematic.Core.Mvc
{
    public class ForgotPasswordController : Controller
    {
        protected readonly IEmailValidatorService EmailValidatorService;
        protected readonly IEmailSenderService EmailSenderService;
        protected readonly IForgotPasswordEmail<User> ForgotPasswordEmail;
        protected readonly IUserRepository<User, UserFilter, UserSpecification> UserRepository;
        protected readonly IStringLocalizer<ForgotPasswordViewModel> Localizer;
        
        protected User AuthenticationUser;

        public ForgotPasswordController(
            IEmailValidatorService emailValidatorService,
            IEmailSenderService emailSenderService,
            IForgotPasswordEmail<User> forgotPasswordEmail,
            IUserRepository<User, UserFilter, UserSpecification> userRepository,
            IStringLocalizer<ForgotPasswordViewModel> localizer)
        {
            EmailValidatorService = emailValidatorService;
            EmailSenderService = emailSenderService;
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

            if (!EmailValidatorService.IsValidEmail(data.Email))
            {
                ModelState.AddModelError("InvalidEmail", Localizer[AuthenticationErrorMessages.InvalidEmail]);
                return PartialView(data);
            }

            var userSpecification = new UserSpecification()
            {
                Email = data.Email
            };

            AuthenticationUser = await UserRepository.ReadAsync(userSpecification);

            if (AuthenticationUser == null)
            {
                ModelState.AddModelError("UserDoesNotExist", Localizer[AuthenticationErrorMessages.UserDoesNotExist]);
                return PartialView(data);
            }

            string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var saveToken = await UserRepository.SaveTokenAsync(AuthenticationUser, token);

            if (!saveToken)
            {
                ModelState.AddModelError("ReminderFailed", Localizer[AuthenticationErrorMessages.PasswordReminderFailed]);
                return PartialView(data);
            }

            var domain = Request.Host.Value;
            domain += (Request.PathBase.Value.HasValue()) ? Request.PathBase.Value : string.Empty;
            var emailSubject = ForgotPasswordEmail.Subject();
            var emailBody = ForgotPasswordEmail.Body(AuthenticationUser, domain, emailSubject, token);

            await EmailSenderService.SendEmailAsync(data.Email, emailSubject, emailBody);

            data.SendReminderSuccess = true;

            return PartialView(data);
        }
    }
}