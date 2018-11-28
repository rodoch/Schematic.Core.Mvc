using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Schematic.Identity;

namespace Schematic.Core.Mvc
{
    public class UserViewModel<TUser> where TUser : ISchematicUser
    {
        public int ResourceID { get; set; }

        public TUser Resource { get; set; }

        public bool UserVerificationRequired { get; set; } = false;

        public bool CanEditPassword { get; set; } = false;
        
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        public string ConfirmPassword { get; set; }
    }
}