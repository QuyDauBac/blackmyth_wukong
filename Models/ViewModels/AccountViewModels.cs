using System.ComponentModel.DataAnnotations;

namespace WukongWeb.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(254, ErrorMessage = "Email must be 254 characters or fewer.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(128, MinimumLength = 10, ErrorMessage = "Use a password between 10 and 128 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        public string ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Display name is required.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Display name must be between 2 and 80 characters.")]
        [Display(Name = "Display name")]
        public string DisplayName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(254, ErrorMessage = "Email must be 254 characters or fewer.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(128, MinimumLength = 10, ErrorMessage = "Use a password between 10 and 128 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; }
    }

    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Display name is required.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Display name must be between 2 and 80 characters.")]
        [Display(Name = "Display name")]
        public string DisplayName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(254)]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(128, MinimumLength = 10, ErrorMessage = "Use a password between 10 and 128 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm the new password.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm new password")]
        public string ConfirmPassword { get; set; }
    }
}
