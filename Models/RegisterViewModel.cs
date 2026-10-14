using System.ComponentModel.DataAnnotations;

namespace BYPManagementSystem.Models
{
    public class RegisterViewModel
    {
        // =========================================================
        // EMAIL
        // =========================================================

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;


        // =========================================================
        // PASSWORD
        // =========================================================

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(
            100,
            MinimumLength = 6,
            ErrorMessage = "Password must be at least 6 characters long."
        )]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;


        // =========================================================
        // CONFIRM PASSWORD
        // =========================================================

        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare(
            nameof(Password),
            ErrorMessage = "The passwords do not match."
        )]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;


        // =========================================================
        // RETURN URL
        // =========================================================

        public string? ReturnUrl { get; set; }
    }
}