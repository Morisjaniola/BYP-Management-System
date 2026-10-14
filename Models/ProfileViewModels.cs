using System.ComponentModel.DataAnnotations;

namespace BYPManagementSystem.Models
{
    // ============================================================
    // PROFILE
    // ============================================================

    public class ProfileViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        public bool EmailConfirmed { get; set; }

        public string Role { get; set; } = string.Empty;
    }


    // ============================================================
    // CHANGE PASSWORD
    // ============================================================

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;


        [Required]
        [StringLength(
            100,
            MinimumLength = 6,
            ErrorMessage = "The new password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;


        [Required]
        [DataType(DataType.Password)]
        [Compare(
            "NewPassword",
            ErrorMessage = "The new password and confirmation password do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}