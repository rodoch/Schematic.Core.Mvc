using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Schematic.Identity;

namespace Schematic.Core.Mvc
{
    [Route("{culture}/user")]
    [Authorize]
    public class UserController<TUser> : Controller where TUser : ISchematicUser, new()
    {
        protected readonly IPasswordValidator PasswordValidator;
        protected readonly IPasswordHasher<TUser> PasswordHasher;
        protected readonly IEmailValidator EmailValidator;
        protected readonly IEmailSender EmailSender;
        protected readonly IUserInvitationEmail<TUser> UserInvitationEmail;
        protected readonly IUserRepository<TUser, UserFilter> UserRepository;
        protected readonly IUserRoleRepository<UserRole> UserRoleRepository;
        protected readonly IStringLocalizer<TUser> Localizer;

        public UserController(
            IPasswordValidator passwordValidator,
            IPasswordHasher<TUser> passwordHasher,
            IEmailValidator emailValidator,
            IEmailSender emailSender,
            IUserInvitationEmail<TUser> userInvitationEmail,
            IUserRepository<TUser, UserFilter> userRepository,
            IUserRoleRepository<UserRole> userRoleRepository,
            IStringLocalizer<TUser> localizer)
        {
            PasswordValidator = passwordValidator;
            PasswordHasher = passwordHasher;
            EmailValidator = emailValidator;
            EmailSender = emailSender;
            UserInvitationEmail = userInvitationEmail;
            UserRepository = userRepository;
            UserRoleRepository = userRoleRepository;
            Localizer = localizer;
        }
        
        protected ClaimsIdentity ClaimsIdentity => User.Identity as ClaimsIdentity;
        protected int UserID => int.Parse(ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        protected bool CanEditPassword(TUser user) => (user.ID == UserID) ? true : false;

        [HttpGet]
        public IActionResult Explorer(int id = 0)
        {
            var explorer = new ResourceExplorerModel()
            {
                ResourceID = id,
                ResourceType = typeof(TUser).Name.ToLower()
            };

            ViewData["ResourceName"] = "Users";

            return View(explorer);
        }

        [Route("create")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {   
            var result = new UserViewModel<TUser>() 
            { 
                Resource = new TUser()
            };

            result.Resource.Roles = await UserRoleRepository.ListAsync() ?? new List<UserRole>();

            return PartialView("_Editor", result);
        }

        [Route("create")]
        [HttpPost]
        public async Task<IActionResult> Create(UserViewModel<TUser> data)
        {
            var roles = await UserRoleRepository.ListAsync();

            // populate the role list data not returned in post request 
            foreach (var userRole in data.Resource.Roles)
            {
                var role = roles.Where(r => r.ID == userRole.ID).FirstOrDefault();
                userRole.Name = role.Name;
                userRole.DisplayTitle = role.DisplayTitle;
            }
            
            string email = data.Resource.Email;

            // validate user e-mail address
            if (email.HasValue())
            {
                if (!EmailValidator.IsValidEmail(email))
                {
                    ModelState.AddModelError("InvalidEmail", Localizer[UserErrorMessages.InvalidEmail]);
                }

                var duplicateUser = await UserRepository.ReadByEmailAsync(email);

                if (duplicateUser != null)
                {
                    ModelState.AddModelError("DuplicateUser", Localizer[UserErrorMessages.DuplicateUser]);
                }
            }

            if (!ModelState.IsValid)
            {
                return PartialView("_Editor", data);
            }

            // create token for new user verification
            string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

            int newResourceID = await UserRepository.CreateAsync(data.Resource, token, UserID);

            if (newResourceID == 0)
            {
                return NoContent();
            }

            var domain = Request.Host.Value;
            domain += (Request.PathBase.Value.HasValue()) ? Request.PathBase.Value : string.Empty;
            var emailSubject = UserInvitationEmail.Subject();
            var emailBody = UserInvitationEmail.Body(data.Resource, domain, emailSubject, token);

            await EmailSender.SendEmailAsync(email, emailSubject, emailBody);

            return Created(Url.Action("Read", "User", new { id = newResourceID }), newResourceID);
        }

        [Route("read")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Read(int id)
        {
            TUser resource = await UserRepository.ReadAsync(id);

            if (resource == null)
            {
                return NotFound();
            }
            
            var result = new UserViewModel<TUser>() 
            { 
                ResourceID = id,
                Resource = resource
            };

            if (CanEditPassword(resource))
            {
                result.CanEditPassword = true;
            }

            if (!resource.PassHash.HasValue())
            {
                result.UserVerificationRequired = true;
            }

            return PartialView("_Editor", result);
        }

        [Route("update")]
        [HttpPost]
        public async Task<IActionResult> Update(UserViewModel<TUser> data)
        {
            var savedData = await UserRepository.ReadAsync(data.Resource.ID);
            var roles = await UserRoleRepository.ListAsync();

            foreach (var userRole in data.Resource.Roles)
            {
                var role = roles.Where(r => r.ID == userRole.ID).FirstOrDefault();
                userRole.Name = role.Name;
                userRole.DisplayTitle = role.DisplayTitle;
            }
            
            string email = data.Resource.Email;

            if (email.HasValue())
            {
                if (!EmailValidator.IsValidEmail(email))
                {
                    ModelState.AddModelError("InvalidEmail", Localizer[UserErrorMessages.InvalidEmail]);
                }

                if (email != savedData.Email)
                {
                    var duplicateUser = await UserRepository.ReadByEmailAsync(email);

                    if (duplicateUser != null)
                    {
                        ModelState.AddModelError("DuplicateUser", Localizer[UserErrorMessages.DuplicateUser]);
                    }
                }
            }

            if (CanEditPassword(data.Resource))
            {
                data.CanEditPassword = true;
            }

            if (!savedData.PassHash.HasValue())
            {
                data.UserVerificationRequired = true;
            }

            if (CanEditPassword(data.Resource) && data.Password.HasValue()
                || CanEditPassword(data.Resource) && data.ConfirmPassword.HasValue())
            {
                if (!data.Password.HasValue() || !data.ConfirmPassword.HasValue())
                {
                    ModelState.AddModelError("Invalid", Localizer[UserErrorMessages.TwoPasswordsRequired]);
                    return PartialView("_Editor", data);
                }

                if (data.Password != data.ConfirmPassword)
                {
                    ModelState.AddModelError("Invalid", Localizer[UserErrorMessages.PasswordsDoNotMatch]);
                    return PartialView("_Editor", data);
                }

                var passwordValidationErrors = PasswordValidator.ValidatePassword(data.Password);

                if (passwordValidationErrors.Count > 0)
                {
                    ModelState.AddModelError("PasswordValidationErrors", 
                        Localizer[PasswordValidator.GetPasswordValidationErrorMessage()]);
                    return PartialView("_Editor", data);
                }

                string passHash = PasswordHasher.HashPassword(data.Resource, data.Password);
                data.Resource.PassHash = passHash;
            }
            else
            {
                data.Resource.PassHash = savedData.PassHash;
            }

            if (!ModelState.IsValid)
            {
                return PartialView("_Editor", data);
            }

            int update = await UserRepository.UpdateAsync(data.Resource, UserID);

            if (update <= 0)
            {
                return BadRequest();
            }

            var updatedResource = await UserRepository.ReadAsync(data.ResourceID);
            
            var result = new UserViewModel<TUser>() 
            { 
                ResourceID = data.ResourceID,
                Resource = updatedResource
            };

            if (CanEditPassword(updatedResource))
            {
                result.CanEditPassword = true;
            }

            if (!updatedResource.PassHash.HasValue())
            {
                result.UserVerificationRequired = true;
            }
            
            return PartialView("_Editor", result);
        }

        [Route("delete")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {   
            int delete = await UserRepository.DeleteAsync(id, UserID);

            if (delete <= 0)
            {
                return BadRequest();
            }

            return NoContent();
        }

        [Route("filter")]
        [HttpGet]
        public IActionResult Filter()
        {
            UserFilter filter = new UserFilter();
            return PartialView("_ResourceFilter", filter);
        }

        [Route("list")]
        [HttpPost]
        public async Task<IActionResult> List(UserFilter filter)
        {
            List<TUser> list = await UserRepository.ListAsync(filter);

            if (list.Count == 0)
            {
                return NoContent();
            }

            var resourceList = new ResourceListModel<TUser>()
            {
                List = list,
                ActiveResourceID = filter.ActiveResourceID
            };

            return PartialView("_ResourceList", resourceList);
        }

        [Route("invite")]
        [HttpPost]
        public async Task<IActionResult> Invite(int userID)
        {
            TUser resource = await UserRepository.ReadAsync(userID);

            var domain = Request.Host.Value;
            domain += (Request.PathBase.Value.HasValue()) ? Request.PathBase.Value : string.Empty;
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var emailSubject = UserInvitationEmail.Subject();
            var emailBody = UserInvitationEmail.Body(resource, domain, emailSubject, token);

            await EmailSender.SendEmailAsync(resource.Email, emailSubject, emailBody);

            return Ok();
        }
    }
}