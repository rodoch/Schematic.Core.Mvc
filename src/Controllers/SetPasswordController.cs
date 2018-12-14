using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Schematic.Identity;

namespace Schematic.Core.Mvc
{
    public class SetPasswordController : Controller
    {
        protected readonly IEmailValidatorService EmailValidatorService;
        protected readonly IPasswordValidatorService PasswordValidatorService;
        protected readonly IPasswordHasherService<User> PasswordHasherService;
        protected readonly IUserRepository<User, UserFilter, UserSpecification> UserRepository;
        protected readonly IStringLocalizer<SetPasswordViewModel> Localizer;

        protected User AuthenticationUser;

        public SetPasswordController(
            IEmailValidatorService emailValidatorService,
            IPasswordValidatorService passwordValidatorService,
            IPasswordHasherService<User> passwordHasherService,
            IUserRepository<User, UserFilter, UserSpecification> userRepository,
            IStringLocalizer<SetPasswordViewModel> localizer)
        {
            EmailValidatorService = emailValidatorService;
            PasswordValidatorService = passwordValidatorService;
            PasswordHasherService = passwordHasherService;
            UserRepository = userRepository;
            Localizer = localizer;
        }

        [BindProperty]
        public SetPasswordViewModel SetPasswordData { get; set; }

        [Route("{culture}/in/set")]
        [HttpGet]
        public IActionResult Authentication(string token)
        {
            var data = new AuthenticationViewModel();
            data.Mode = "set-password";
            data.Token = token;

            return View("/Views/Authentication/Authentication.cshtml", data);
        }

        [Route("{culture}/in/set-password")]
        [HttpGet]
        public IActionResult SetPassword(string token)
        {
            var data = new SetPasswordViewModel();
            data.Token = token;

            return PartialView(data);
        }

        [Route("{culture}/in/set-password")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel data)
        {
            ViewData["Email"] = data.Email;

            if (!ModelState.IsValid)
            {
                return PartialView(data);
            }

            if (!EmailValidatorService.IsValidEmail(data.Email))
            {
                ModelState.AddModelError("InvalidEmail", 
                    Localizer[AuthenticationErrorMessages.InvalidEmail]);
                return PartialView(data);
            }

            if (data.NewPassword.HasValue() && data.ConfirmNewPassword.HasValue() 
                && data.NewPassword != data.ConfirmNewPassword)
            {
                ModelState.AddModelError("PasswordsDoNotMatch", 
                    Localizer[AuthenticationErrorMessages.PasswordsDoNotMatch]);
                return PartialView(data);
            }

            var tokenResult = await UserRepository.ValidateTokenAsync(data.Email, data.Token);

            if (tokenResult == TokenVerificationResult.Invalid)
            {
                ModelState.AddModelError("InvalidToken", 
                    Localizer[AuthenticationErrorMessages.InvalidToken]);
                return PartialView(data);
            }

            if (tokenResult == TokenVerificationResult.Expired)
            {
                ModelState.AddModelError("ExpiredToken", 
                    Localizer[AuthenticationErrorMessages.ExpiredToken]);
                return PartialView(data);
            }

            var userSpecification = new UserSpecification()
            {
                Email = data.Email
            };

            AuthenticationUser = await UserRepository.ReadAsync(userSpecification);

            if (AuthenticationUser == null)
            {
                ModelState.AddModelError("UserDoesNotExist", 
                    Localizer[AuthenticationErrorMessages.UserDoesNotExist]);
                return PartialView(data);
            }

            var passwordValidationErrors = PasswordValidatorService.ValidatePassword(data.NewPassword);

            if (passwordValidationErrors.Count > 0)
            {
                ModelState.AddModelError("PasswordValidationErrors", 
                    Localizer[PasswordValidatorService.GetPasswordValidationErrorMessage()]);
                return PartialView(data);
            }

            string passHash = PasswordHasherService.HashPassword(AuthenticationUser, data.NewPassword);
            var setPassHash = await UserRepository.SetPasswordAsync(AuthenticationUser, passHash);

            if (!setPassHash)
            {
                ModelState.AddModelError("PasswordSetFailed", 
                    Localizer[AuthenticationErrorMessages.PasswordSetFailed]);
                return PartialView();
            }

            data.SetPasswordSuccess = true;

            return PartialView(data);
        }
    }
}