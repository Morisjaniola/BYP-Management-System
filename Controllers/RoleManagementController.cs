using System.ComponentModel.DataAnnotations;
using BYPManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BYPManagementSystem.Controllers
{
    // ============================================================
    // ROLE MANAGEMENT CONTROLLER
    // ADMIN ONLY
    // ============================================================

    [Authorize(Roles = "Admin")]
    public class RoleManagementController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;

        public RoleManagementController(
            RoleManager<IdentityRole> roleManager,
            UserManager<IdentityUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }


        // ==========================================================
        // INDEX
        // VIEW ROLES
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search)
        {
            var roles =
                await _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .ToListAsync();


            var roleRows =
                new List<RoleManagementRowViewModel>();


            // ------------------------------------------------------
            // BUILD ROLE INFORMATION
            // ------------------------------------------------------

            foreach (var role in roles)
            {
                var roleName =
                    role.Name ?? string.Empty;


                var users =
                    await _userManager.GetUsersInRoleAsync(
                        roleName
                    );


                var isSystemRole =
                    roleName == "Admin" ||
                    roleName == "Staff" ||
                    roleName == "User";


                // --------------------------------------------------
                // SEARCH
                // --------------------------------------------------

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchValue =
                        search.Trim();

                    if (!roleName.Contains(
                        searchValue,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }


                roleRows.Add(
                    new RoleManagementRowViewModel
                    {
                        Id =
                            role.Id,

                        Name =
                            roleName,

                        UserCount =
                            users.Count,

                        IsSystemRole =
                            isSystemRole
                    }
                );
            }


            ViewBag.Search =
                search;


            ViewBag.TotalRoles =
                roles.Count;


            ViewBag.SystemRoles =
                roles.Count(
                    r =>
                        r.Name == "Admin" ||
                        r.Name == "Staff" ||
                        r.Name == "User"
                );


            ViewBag.CustomRoles =
                roles.Count(
                    r =>
                        r.Name != "Admin" &&
                        r.Name != "Staff" &&
                        r.Name != "User"
                );


            return View(roleRows);
        }


        // ==========================================================
        // CREATE ROLE - GET
        // ==========================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View(
                new RoleInputViewModel()
            );
        }


        // ==========================================================
        // CREATE ROLE - POST
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            RoleInputViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var roleName =
                model.Name.Trim();


            // ------------------------------------------------------
            // CHECK ROLE NAME
            // ------------------------------------------------------

            if (string.IsNullOrWhiteSpace(roleName))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Role name is required."
                );

                return View(model);
            }


            // ------------------------------------------------------
            // PREVENT DUPLICATE ROLE
            // ------------------------------------------------------

            if (await _roleManager.RoleExistsAsync(
                roleName))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "A role with this name already exists."
                );

                return View(model);
            }


            // ------------------------------------------------------
            // CREATE
            // ------------------------------------------------------

            var result =
                await _roleManager.CreateAsync(
                    new IdentityRole(roleName)
                );


            if (!result.Succeeded)
            {
                foreach (var error
                    in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                return View(model);
            }


            TempData["Success"] =
                $"Role '{roleName}' was successfully created.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // EDIT ROLE - GET
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }


            var role =
                await _roleManager.FindByIdAsync(
                    id
                );


            if (role == null)
            {
                return NotFound();
            }


            var roleName =
                role.Name
                ?? string.Empty;


            var userCount =
                (await _userManager.GetUsersInRoleAsync(
                    roleName
                )).Count;


            var model =
                new RoleEditViewModel
                {
                    Id =
                        role.Id,

                    Name =
                        roleName,

                    OriginalName =
                        roleName,

                    UserCount =
                        userCount,

                    IsSystemRole =
                        IsSystemRole(roleName)
                };


            return View(model);
        }


        // ==========================================================
        // EDIT ROLE - POST
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            RoleEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var role =
                await _roleManager.FindByIdAsync(
                    model.Id
                );


            if (role == null)
            {
                return NotFound();
            }


            var oldName =
                role.Name
                ?? string.Empty;


            var newName =
                model.Name.Trim();


            // ------------------------------------------------------
            // SYSTEM ROLE PROTECTION
            // ------------------------------------------------------

            if (IsSystemRole(oldName))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    $"The system role '{oldName}' cannot be renamed."
                );

                model.OriginalName =
                    oldName;

                model.IsSystemRole =
                    true;

                model.UserCount =
                    (await _userManager.GetUsersInRoleAsync(
                        oldName
                    )).Count;

                return View(model);
            }


            // ------------------------------------------------------
            // VALIDATE NAME
            // ------------------------------------------------------

            if (string.IsNullOrWhiteSpace(newName))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Role name is required."
                );

                return View(model);
            }


            // ------------------------------------------------------
            // PREVENT USING RESERVED SYSTEM ROLE
            // ------------------------------------------------------

            if (IsSystemRole(newName))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Admin, Staff, and User are reserved system roles."
                );

                return View(model);
            }


            // ------------------------------------------------------
            // DUPLICATE ROLE
            // ------------------------------------------------------

            var duplicateRole =
                await _roleManager.FindByNameAsync(
                    newName
                );


            if (duplicateRole != null &&
                duplicateRole.Id != role.Id)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "A role with this name already exists."
                );

                return View(model);
            }


            // ------------------------------------------------------
            // RENAME ROLE
            // ------------------------------------------------------

            role.Name =
                newName;

            role.NormalizedName =
                _roleManager.NormalizeKey(
                    newName
                );


            var result =
                await _roleManager.UpdateAsync(
                    role
                );


            if (!result.Succeeded)
            {
                foreach (var error
                    in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                return View(model);
            }


            TempData["Success"] =
                $"Role '{oldName}' was renamed to '{newName}'.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // DELETE ROLE
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }


            var role =
                await _roleManager.FindByIdAsync(
                    id
                );


            if (role == null)
            {
                return NotFound();
            }


            var roleName =
                role.Name
                ?? string.Empty;


            // ------------------------------------------------------
            // NEVER DELETE SYSTEM ROLES
            // ------------------------------------------------------

            if (IsSystemRole(roleName))
            {
                TempData["Error"] =
                    $"The system role '{roleName}' cannot be deleted.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // ------------------------------------------------------
            // CHECK USERS
            // ------------------------------------------------------

            var users =
                await _userManager.GetUsersInRoleAsync(
                    roleName
                );


            if (users.Count > 0)
            {
                TempData["Error"] =
                    $"Role '{roleName}' cannot be deleted because {users.Count} user account(s) are currently assigned to it.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // ------------------------------------------------------
            // DELETE
            // ------------------------------------------------------

            var result =
                await _roleManager.DeleteAsync(
                    role
                );


            if (!result.Succeeded)
            {
                TempData["Error"] =
                    "The selected role could not be deleted.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            TempData["Success"] =
                $"Role '{roleName}' was successfully deleted.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // ASSIGN ROLE TO USER
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToUser(
            string userId,
            string roleName)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(roleName))
            {
                TempData["Error"] =
                    "A user and role are required.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            var user =
                await _userManager.FindByIdAsync(
                    userId
                );


            if (user == null)
            {
                TempData["Error"] =
                    "The selected user account was not found.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            if (!await _roleManager.RoleExistsAsync(
                roleName))
            {
                TempData["Error"] =
                    "The selected role does not exist.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            var currentRoles =
                await _userManager.GetRolesAsync(
                    user
                );


            if (currentRoles.Contains(
                roleName,
                StringComparer.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    $"User '{user.Email}' already has the '{roleName}' role.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // ------------------------------------------------------
            // LAST ACTIVE ADMIN SAFEGUARD (PHASE 2)
            //
            // This action replaces the user's current roles. Never
            // allow the Admin role to be taken away from the last
            // remaining active administrator.
            // ------------------------------------------------------

            var losesAdminRole =
                currentRoles.Any(r =>
                    r.Equals(
                        "Admin",
                        StringComparison
                            .OrdinalIgnoreCase))

                &&

                !roleName.Equals(
                    "Admin",
                    StringComparison
                        .OrdinalIgnoreCase);


            if (losesAdminRole &&
                await IsLastRemainingActiveAdministratorAsync(
                    userId))
            {
                TempData["Error"] =
                    "Cannot change this user's role: they are the last remaining active administrator. Assign another administrator first.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // ------------------------------------------------------
            // REMOVE EXISTING ROLES
            // ------------------------------------------------------

            if (currentRoles.Count > 0)
            {
                var removeResult =
                    await _userManager.RemoveFromRolesAsync(
                        user,
                        currentRoles
                    );


                if (!removeResult.Succeeded)
                {
                    TempData["Error"] =
                        "The user's previous roles could not be removed.";

                    return RedirectToAction(
                        nameof(Index)
                    );
                }
            }


            // ------------------------------------------------------
            // ADD NEW ROLE
            // ------------------------------------------------------

            var addResult =
                await _userManager.AddToRoleAsync(
                    user,
                    roleName
                );


            if (!addResult.Succeeded)
            {
                TempData["Error"] =
                    "The new role could not be assigned.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            TempData["Success"] =
                $"User '{user.Email}' was assigned the '{roleName}' role.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // HELPERS
        // ==========================================================

        // ==========================================================
        // LAST ACTIVE ADMIN HELPER (PHASE 2)
        // ==========================================================
        //
        // Returns true when the given user is an ACTIVE administrator
        // and no OTHER active administrator remains.
        // ==========================================================

        private async Task<bool> IsLastRemainingActiveAdministratorAsync(
            string userId)
        {
            var admins =
                await _userManager.GetUsersInRoleAsync(
                    "Admin"
                );


            var now =
                DateTimeOffset.UtcNow;


            return !admins.Any(a =>
                a.Id != userId

                &&

                (
                    !a.LockoutEnd.HasValue

                    ||

                    a.LockoutEnd.Value <= now
                ));
        }


        private static bool IsSystemRole(
            string roleName)
        {
            return roleName.Equals(
                       "Admin",
                       StringComparison.OrdinalIgnoreCase
                   )
                   ||
                   roleName.Equals(
                       "Staff",
                       StringComparison.OrdinalIgnoreCase
                   )
                   ||
                   roleName.Equals(
                       "User",
                       StringComparison.OrdinalIgnoreCase
                   );
        }


        // ==========================================================
        // VIEW MODELS
        // ==========================================================

        public class RoleManagementRowViewModel
        {
            public string Id { get; set; }
                = string.Empty;

            public string Name { get; set; }
                = string.Empty;

            public int UserCount { get; set; }

            public bool IsSystemRole { get; set; }
        }


        public class RoleInputViewModel
        {
            [Required]
            [StringLength(
                50,
                MinimumLength = 2,
                ErrorMessage =
                    "Role name must be between 2 and 50 characters."
            )]
            public string Name { get; set; }
                = string.Empty;
        }


        public class RoleEditViewModel
        {
            [Required]
            public string Id { get; set; }
                = string.Empty;


            [Required]
            [StringLength(
                50,
                MinimumLength = 2,
                ErrorMessage =
                    "Role name must be between 2 and 50 characters."
            )]
            public string Name { get; set; }
                = string.Empty;


            public string OriginalName { get; set; }
                = string.Empty;


            public int UserCount { get; set; }

            public bool IsSystemRole { get; set; }
        }
    }
}