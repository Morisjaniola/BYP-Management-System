using System.ComponentModel.DataAnnotations;

namespace BYPManagementSystem.Models
{
    // ============================================================
    // USER MANAGEMENT PAGE
    // ============================================================

    public class UserManagementViewModel
    {
        public string? Search { get; set; }

        public string? SelectedRole { get; set; }

        public List<string> Roles { get; set; } = new();

        public List<UserListItemViewModel> Users { get; set; } = new();
    }


    // ============================================================
    // USER LIST ITEM
    // ============================================================

    public class UserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new();

        public bool IsActive { get; set; }

        public string RoleDisplay =>
            Roles.Count > 0
                ? string.Join(", ", Roles)
                : "No Role";
    }


    // ============================================================
    // EDIT USER
    // ============================================================

    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role")]
        public string RoleName { get; set; } = string.Empty;

        [Display(Name = "Account Active")]
        public bool IsActive { get; set; }

        public List<string> AvailableRoles { get; set; } = new();
    }


    // ============================================================
    // ROLE MANAGEMENT
    // ============================================================

    public class RoleManagementViewModel
    {
        public List<RoleListItemViewModel> Roles { get; set; } = new();
    }


    public class RoleListItemViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int UserCount { get; set; }

        public bool IsSystemRole { get; set; }
    }


    // ============================================================
    // CREATE ROLE
    // ============================================================

    public class CreateRoleViewModel
    {
        [Required]
        [StringLength(50)]
        [Display(Name = "Role Name")]
        public string RoleName { get; set; } = string.Empty;
    }
}