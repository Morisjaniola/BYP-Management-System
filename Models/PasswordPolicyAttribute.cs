using System.ComponentModel.DataAnnotations;

namespace BYPManagementSystem.Models
{
    // =========================================================
    // PASSWORD POLICY VALIDATION
    // =========================================================
    // Mirrors the ASP.NET Core Identity password policy configured
    // in Program.cs:
    //
    //   RequiredLength          = 8
    //   RequireUppercase        = true
    //   RequireLowercase        = true
    //   RequireDigit            = true
    //   RequireNonAlphanumeric  = false
    //
    // Keeping the rules in one attribute means the registration
    // form rejects the same passwords that UserManager.CreateAsync
    // would reject, and can report the specific rule that failed
    // instead of a generic Identity error.
    //
    // The minimum length itself is enforced by RegisterViewModel's
    // [StringLength] attribute so the browser still receives the
    // standard data-val-length/minlength rules. This attribute only
    // checks character classes once the password is long enough.
    // =========================================================

    [AttributeUsage(
        AttributeTargets.Property |
        AttributeTargets.Field |
        AttributeTargets.Parameter,
        AllowMultiple = false)]
    public sealed class PasswordPolicyAttribute : ValidationAttribute
    {
        public const int MinimumLength = 8;

        protected override ValidationResult? IsValid(
            object? value,
            ValidationContext validationContext)
        {
            // [Required] reports empty passwords.
            if (value is not string password || password.Length == 0)
            {
                return ValidationResult.Success;
            }

            // A too-short password already has the clearer
            // StringLength message; avoid stacking several
            // character-class errors on top of it.
            if (password.Length < MinimumLength)
            {
                return ValidationResult.Success;
            }

            if (!password.Any(char.IsUpper))
            {
                return new ValidationResult(
                    "Password must include at least one uppercase letter."
                );
            }

            if (!password.Any(char.IsLower))
            {
                return new ValidationResult(
                    "Password must include at least one lowercase letter."
                );
            }

            if (!password.Any(char.IsDigit))
            {
                return new ValidationResult(
                    "Password must include at least one number."
                );
            }

            return ValidationResult.Success;
        }
    }
}
