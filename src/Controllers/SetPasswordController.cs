using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Schematic.Identity;

namespace Schematic.Core.Mvc
{
    public class SetPasswordController : Controller
    {
        protected readonly IEmailValidator EmailValidator;
        protected readonly IPasswordValidator PasswordValidator;
        protected readonly IPasswordHasher<User> PasswordHasher;
        protected readonly IUserRepository<User, UserFilter> UserRepository;
        protected readonly IStringLocalizer<SetPasswordViewModel> Localizer;

        protected User AuthenticationUser;

        public SetPasswordController(
            IEmailValidator emailValidator,
            IPasswordValidator passwordValidator,
            IPasswordHasher<User> passwordHasher,
            IUserRepository<User, UserFilter> userRepository,
            IStringLocalizer<SetPasswordViewModel> localizer)
        {
            EmailValidator = emailValidator;
            PasswordValidator = passwordValidator;
            PasswordHasher = passwordHasher;
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
        public IActionResult SetPassword(SetPasswordViewModel data)
        {
            ViewData["Email"] = data.Email;

            if (!ModelState.IsValid)
            {
                return PartialView(data);
            }

            if (!EmailValidator.IsValidEmail(data.Email))
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

            var tokenResult = UserRepository.ValidateToken(data.Email, data.Token);

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

            AuthenticationUser = UserRepository.ReadByEmail(data.Email);

            if (AuthenticationUser == null)
            {
                ModelState.AddModelError("UserDoesNotExist", 
                    Localizer[AuthenticationErrorMessages.UserDoesNotExist]);
                return PartialView(data);
            }

            var passwordValidationErrors = PasswordValidator.ValidatePassword(data.NewPassword);

            if (passwordValidationErrors.Count > 0)
            {
                ModelState.AddModelError("PasswordValidationErrors", 
                    Localizer[PasswordValidator.GetPasswordValidationErrorMessage()]);
                return PartialView(data);
            }

            string passHash = PasswordHasher.HashPassword(AuthenticationUser, data.NewPassword);
            var setPassHash = UserRepository.SetPassword(AuthenticationUser, passHash);

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