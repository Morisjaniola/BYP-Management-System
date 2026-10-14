using System.ComponentModel.DataAnnotations;

namespace BYPManagementSystem.Models
{
    public class LoginViewModel
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
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;


        // =========================================================
        // REMEMBER ME
        // =========================================================

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }


        // =========================================================
        // RETURN URL
        // =========================================================

        public string? ReturnUrl { get; set; }
    }
}