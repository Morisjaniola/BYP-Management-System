using System.ComponentModel.DataAnnotations;
using BYPManagementSystem.Data;
using BYPManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BYPManagementSystem.Controllers
{
    // ============================================================
    // USER MANAGEMENT CONTROLLER
    // ADMIN ONLY
    // ============================================================

    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;

        public UserManagementController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _environment = environment;
        }


        // ==========================================================
        // INDEX
        // VIEW USERS
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? role,
            string? status)
        {
            var users =
                await _userManager.Users
                    .AsNoTracking()
                    .OrderBy(u => u.Email)
                    .ToListAsync();


            // ------------------------------------------------------
            // ROLE FILTERING
            // ------------------------------------------------------

            var userRows =
                new List<UserManagementRowViewModel>();


            foreach (var user in users)
            {
                var roles =
                    await _userManager.GetRolesAsync(user);

                var userRole =
                    roles.FirstOrDefault()
                    ?? "User";


                var isActive =
                    user.LockoutEnd == null ||
                    user.LockoutEnd <= DateTimeOffset.UtcNow;


                // --------------------------------------------------
                // SEARCH
                // --------------------------------------------------

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchValue =
                        search.Trim();

                    var matches =
                        (user.Email?.Contains(
                            searchValue,
                            StringComparison.OrdinalIgnoreCase
                        ) ?? false)
                        ||
                        (user.UserName?.Contains(
                            searchValue,
                            StringComparison.OrdinalIgnoreCase
                        ) ?? false)
                        ||
                        user.Id.Contains(
                            searchValue,
                            StringComparison.OrdinalIgnoreCase
                        );

                    if (!matches)
                    {
                        continue;
                    }
                }


                // --------------------------------------------------
                // ROLE FILTER
                // --------------------------------------------------

                if (!string.IsNullOrWhiteSpace(role) &&
                    !string.Equals(
                        role,
                        userRole,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }


                // --------------------------------------------------
                // STATUS FILTER
                // --------------------------------------------------

                if (!string.IsNullOrWhiteSpace(status))
                {
                    var wantedActive =
                        status.Equals(
                            "Active",
                            StringComparison.OrdinalIgnoreCase
                        );

                    var wantedInactive =
                        status.Equals(
                            "Inactive",
                            StringComparison.OrdinalIgnoreCase
                        );

                    if (wantedActive && !isActive)
                    {
                        continue;
                    }

                    if (wantedInactive && isActive)
                    {
                        continue;
                    }
                }


                // --------------------------------------------------
                // ADD ROW
                // --------------------------------------------------

                userRows.Add(
                    new UserManagementRowViewModel
                    {
                        Id = user.Id,

                        Email =
                            user.Email
                            ?? user.UserName
                            ?? string.Empty,

                        UserName =
                            user.UserName
                            ?? string.Empty,

                        Role =
                            userRole,

                        IsActive =
                            isActive,

                        EmailConfirmed =
                            user.EmailConfirmed,

                        LockoutEnd =
                            user.LockoutEnd
                    }
                );
            }


            // ------------------------------------------------------
            // ROLE LIST
            // ------------------------------------------------------

            var rolesList =
                await _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .Select(r => r.Name!)
                    .ToListAsync();


            ViewBag.Search =
                search;

            ViewBag.Role =
                role;

            ViewBag.Status =
                status;

            ViewBag.Roles =
                rolesList;


            return View(userRows);
        }


        // ==========================================================
        // CREATE USER - GET
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model =
                new UserCreateViewModel();


            await LoadRolesViewBag();


            return View(model);
        }


        // ==========================================================
        // CREATE USER - POST
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            UserCreateViewModel model)
        {
            await LoadRolesViewBag();

            ValidateProfileImage(model.ProfileImage);


            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // ------------------------------------------------------
            // CLEAN INPUT
            // ------------------------------------------------------

            var email =
                model.Email.Trim();


            var role =
                model.Role.Trim();


            // ------------------------------------------------------
            // CHECK EMAIL
            // ------------------------------------------------------

            var existingUser =
                await _userManager.FindByEmailAsync(
                    email
                );

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "An account with this email already exists."
                );

                return View(model);
            }


            // ------------------------------------------------------
            // CHECK ROLE
            // ------------------------------------------------------

            if (!await _roleManager.RoleExistsAsync(role))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "The selected role does not exist."
                );

                return View(model);
            }


            // ------------------------------------------------------
            // CREATE IDENTITY USER
            // ------------------------------------------------------

            var user =
                new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = model.EmailConfirmed
                };


            var createResult =
                await _userManager.CreateAsync(
                    user,
                    model.Password
                );


            if (!createResult.Succeeded)
            {
                foreach (var error
                    in createResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                return View(model);
            }


            // ------------------------------------------------------
            // ASSIGN ROLE
            // ------------------------------------------------------

            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    role
                );


            if (!roleResult.Succeeded)
            {
                // Remove account if role assignment fails
                await _userManager.DeleteAsync(user);

                foreach (var error
                    in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                return View(model);
            }


            // ------------------------------------------------------
            // OPTIONAL YOUTH PARTICIPANT
            // ------------------------------------------------------

            if (role == "User" &&
                !string.IsNullOrWhiteSpace(
                    model.FirstName) &&
                !string.IsNullOrWhiteSpace(
                    model.LastName) &&
                !string.IsNullOrWhiteSpace(
                    model.Barangay))
            {
                var youth =
                    new YouthParticipant
                    {
                        UserId =
                            user.Id,

                        FirstName =
                            model.FirstName.Trim(),

                        LastName =
                            model.LastName.Trim(),

                        BirthDate =
                            model.BirthDate,

                        ContactNumber =
                            string.IsNullOrWhiteSpace(
                                model.ContactNumber)
                                ? null
                                : model.ContactNumber.Trim(),

                        Address =
                            string.IsNullOrWhiteSpace(
                                model.Address)
                                ? null
                                : model.Address.Trim(),

                        Barangay =
                            model.Barangay.Trim(),

                        IsActive = true,

                        CreatedAt =
                            DateTime.UtcNow
                    };


                _context.YouthParticipants.Add(
                    youth
                );
            }

            if (model.ProfileImage is { Length: > 0 })
            {
                var imagePath = await SaveProfileImageAsync(model.ProfileImage);
                var claimResult = await _userManager.AddClaimAsync(user,
                    new Claim(ProfileImageClaimType, imagePath));

                if (!claimResult.Succeeded)
                {
                    foreach (var error in claimResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"User account '{email}' was successfully created.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // EDIT USER - GET
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }


            var user =
                await _userManager.FindByIdAsync(id);


            if (user == null)
            {
                return NotFound();
            }


            var roles =
                await _userManager.GetRolesAsync(user);


            var role =
                roles.FirstOrDefault()
                ?? "User";


            var participant =
                await _context.YouthParticipants
                    .FirstOrDefaultAsync(
                        y => y.UserId == user.Id
                    );


            var model =
                new UserEditViewModel
                {
                    Id =
                        user.Id,

                    Email =
                        user.Email
                        ?? user.UserName
                        ?? string.Empty,

                    Role =
                        role,

                    EmailConfirmed =
                        user.EmailConfirmed,

                    FirstName =
                        participant?.FirstName
                        ?? string.Empty,

                    LastName =
                        participant?.LastName
                        ?? string.Empty,

                    BirthDate =
                        participant?.BirthDate,

                    ContactNumber =
                        participant?.ContactNumber,

                    Address =
                        participant?.Address,

                    Barangay =
                        participant?.Barangay
                        ?? string.Empty,

                    IsActive =
                        user.LockoutEnd == null ||
                        user.LockoutEnd <=
                            DateTimeOffset.UtcNow
                };


            await LoadRolesViewBag();


            return View(model);
        }


        // ==========================================================
        // EDIT USER - POST
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            UserEditViewModel model)
        {
            await LoadRolesViewBag();

            ValidateProfileImage(model.ProfileImage);


            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var user =
                await _userManager.FindByIdAsync(
                    model.Id
                );


            if (user == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // DO NOT REMOVE ADMIN ACCESS FROM YOURSELF
            // ------------------------------------------------------

            if (user.Id ==
                    _userManager.GetUserId(User) &&
                model.Role != "Admin")
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "You cannot remove the Admin role from your own account."
                );

                return View(model);
            }


            // ------------------------------------------------------
            // NORMALIZE INPUT
            // ------------------------------------------------------

            var email =
                model.Email.Trim();

            var role =
                model.Role.Trim();


            // ------------------------------------------------------
            // EMAIL DUPLICATE
            // ------------------------------------------------------

            var existingUser =
                await _userManager.FindByEmailAsync(
                    email
                );


            if (existingUser != null &&
                existingUser.Id != user.Id)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Another account is already using this email."
                );

                return View(model);
            }


            // ------------------------------------------------------
            // ROLE VALIDATION
            // ------------------------------------------------------

            if (!await _roleManager.RoleExistsAsync(role))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "The selected role does not exist."
                );

                return View(model);
            }


            // ------------------------------------------------------
            // UPDATE EMAIL
            // ------------------------------------------------------

            if (!string.Equals(
                    user.Email,
                    email,
                    StringComparison.OrdinalIgnoreCase))
            {
                var emailResult =
                    await _userManager.SetEmailAsync(
                        user,
                        email
                    );


                if (!emailResult.Succeeded)
                {
                    foreach (var error
                        in emailResult.Errors)
                    {
                        ModelState.AddModelError(
                            nameof(model.Email),
                            error.Description
                        );
                    }

                    return View(model);
                }


                var usernameResult =
                    await _userManager.SetUserNameAsync(
                        user,
                        email
                    );


                if (!usernameResult.Succeeded)
                {
                    foreach (var error
                        in usernameResult.Errors)
                    {
                        ModelState.AddModelError(
                            nameof(model.Email),
                            error.Description
                        );
                    }

                    return View(model);
                }
            }


            // ------------------------------------------------------
            // EMAIL CONFIRMATION
            // ------------------------------------------------------

            user.EmailConfirmed =
                model.EmailConfirmed;


            // ------------------------------------------------------
            // UPDATE ROLE
            // ------------------------------------------------------

            var currentRoles =
                await _userManager.GetRolesAsync(
                    user
                );


            if (currentRoles.Count > 0)
            {
                var removeRolesResult =
                    await _userManager.RemoveFromRolesAsync(
                        user,
                        currentRoles
                    );


                if (!removeRolesResult.Succeeded)
                {
                    foreach (var error
                        in removeRolesResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description
                        );
                    }

                    return View(model);
                }
            }


            var addRoleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    role
                );


            if (!addRoleResult.Succeeded)
            {
                foreach (var error
                    in addRoleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                return View(model);
            }


            // ------------------------------------------------------
            // ACTIVE / INACTIVE
            // ------------------------------------------------------

            var currentUserId =
                _userManager.GetUserId(User);


            if (user.Id == currentUserId)
            {
                // Never deactivate the currently logged-in admin.
                user.LockoutEnd = null;
            }
            else
            {
                user.LockoutEnd =
                    model.IsActive
                        ? null
                        : DateTimeOffset.UtcNow.AddYears(100);
            }


            // ------------------------------------------------------
            // USERNAME / EMAIL
            // ------------------------------------------------------

            user.UserName =
                email;

            user.Email =
                email;


            // ------------------------------------------------------
            // YOUTH PROFILE
            // ------------------------------------------------------

            if (role == "User")
            {
                var participant =
                    await _context.YouthParticipants
                        .FirstOrDefaultAsync(
                            y => y.UserId == user.Id
                        );


                var hasProfileInformation =
                    !string.IsNullOrWhiteSpace(
                        model.FirstName)
                    ||
                    !string.IsNullOrWhiteSpace(
                        model.LastName)
                    ||
                    !string.IsNullOrWhiteSpace(
                        model.Barangay);


                if (participant == null &&
                    hasProfileInformation)
                {
                    participant =
                        new YouthParticipant
                        {
                            UserId =
                                user.Id,

                            FirstName =
                                model.FirstName.Trim(),

                            LastName =
                                model.LastName.Trim(),

                            BirthDate =
                                model.BirthDate,

                            ContactNumber =
                                string.IsNullOrWhiteSpace(
                                    model.ContactNumber)
                                    ? null
                                    : model.ContactNumber.Trim(),

                            Address =
                                string.IsNullOrWhiteSpace(
                                    model.Address)
                                    ? null
                                    : model.Address.Trim(),

                            Barangay =
                                model.Barangay.Trim(),

                            IsActive = true,

                            CreatedAt =
                                DateTime.UtcNow
                        };


                    _context.YouthParticipants.Add(
                        participant
                    );
                }
                else if (participant != null)
                {
                    participant.FirstName =
                        model.FirstName.Trim();

                    participant.LastName =
                        model.LastName.Trim();

                    participant.BirthDate =
                        model.BirthDate;

                    participant.ContactNumber =
                        string.IsNullOrWhiteSpace(
                            model.ContactNumber)
                            ? null
                            : model.ContactNumber.Trim();

                    participant.Address =
                        string.IsNullOrWhiteSpace(
                            model.Address)
                            ? null
                            : model.Address.Trim();

                    participant.Barangay =
                        model.Barangay.Trim();

                    participant.IsActive =
                        true;
                }
            }

            if (model.ProfileImage is { Length: > 0 })
            {
                var oldImagePath = await GetProfileImagePathAsync(user);
                var newImagePath = await SaveProfileImageAsync(model.ProfileImage);
                var claimResult = string.IsNullOrWhiteSpace(oldImagePath)
                    ? await _userManager.AddClaimAsync(user, new Claim(ProfileImageClaimType, newImagePath))
                    : await _userManager.ReplaceClaimAsync(user,
                        new Claim(ProfileImageClaimType, oldImagePath),
                        new Claim(ProfileImageClaimType, newImagePath));

                if (!claimResult.Succeeded)
                {
                    DeleteProfileImage(newImagePath);
                    foreach (var error in claimResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                DeleteProfileImage(oldImagePath);
            }


            // ------------------------------------------------------
            // SAVE
            // ------------------------------------------------------

            var updateResult =
                await _userManager.UpdateAsync(
                    user
                );


            if (!updateResult.Succeeded)
            {
                foreach (var error
                    in updateResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                return View(model);
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"User account '{email}' was successfully updated.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // ACTIVATE USER
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(
            string id)
        {
            var user =
                await _userManager.FindByIdAsync(id);


            if (user == null)
            {
                return NotFound();
            }


            user.LockoutEnd =
                null;


            var result =
                await _userManager.UpdateAsync(
                    user
                );


            if (!result.Succeeded)
            {
                TempData["Error"] =
                    "The user account could not be activated.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            TempData["Success"] =
                $"User '{user.Email}' has been activated.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // DEACTIVATE USER
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(
            string id)
        {
            var currentUserId =
                _userManager.GetUserId(User);


            // ------------------------------------------------------
            // PREVENT SELF DEACTIVATION
            // ------------------------------------------------------

            if (id == currentUserId)
            {
                TempData["Error"] =
                    "You cannot deactivate your own account while logged in.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            var user =
                await _userManager.FindByIdAsync(id);


            if (user == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // LAST ACTIVE ADMIN SAFEGUARD (PHASE 2)
            // ------------------------------------------------------

            if (await IsLastRemainingActiveAdministratorAsync(
                    id))
            {
                TempData["Error"] =
                    "Cannot deactivate: this is the last remaining active administrator account. Assign another administrator first.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            user.LockoutEnd =
                DateTimeOffset.UtcNow.AddYears(100);


            var result =
                await _userManager.UpdateAsync(
                    user
                );


            if (!result.Succeeded)
            {
                TempData["Error"] =
                    "The user account could not be deactivated.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            TempData["Success"] =
                $"User '{user.Email}' has been deactivated.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // LAST ACTIVE ADMIN HELPER (PHASE 2)
        // ==========================================================
        //
        // Returns true when the given user is an ACTIVE administrator
        // (not locked out / deactivated) and no OTHER active
        // administrator remains. Used to block deleting or
        // deactivating the last active admin account.
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


        // ==========================================================
        // DELETE USER
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            string id)
        {
            var currentUserId =
                _userManager.GetUserId(User);

            if (id == currentUserId)
            {
                TempData["Error"] =
                    "You cannot delete your own account while logged in.";

                return RedirectToAction(
                    nameof(Index)
                );
            }

            var user =
                await _userManager.FindByIdAsync(id);


            if (user == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // LAST ACTIVE ADMIN SAFEGUARD (PHASE 2)
            // ------------------------------------------------------

            if (await IsLastRemainingActiveAdministratorAsync(
                    id))
            {
                TempData["Error"] =
                    "Cannot delete: this is the last remaining active administrator account. Assign another administrator first.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // ------------------------------------------------------
            // LINKED YOUTH / REGISTRATION HISTORY (PHASE 2)
            //
            // Registrations, attendance, photo submissions, and
            // certificates are protected history: deleting the
            // account would orphan them, so deactivation is
            // recommended instead. A linked participant record with
            // NO history at all can be removed together with the
            // account so no orphaned record is left behind.
            // ------------------------------------------------------

            var linkedParticipantIds =
                await _context.YouthParticipants
                    .Where(
                        p =>
                            p.UserId == id
                    )
                    .Select(
                        p => p.Id
                    )
                    .ToListAsync();


            var hasHistory =
                await _context.ProgramRegistrations
                    .AnyAsync(
                        r =>
                            (
                                r.YouthParticipantId != null

                                &&

                                linkedParticipantIds.Contains(
                                    r.YouthParticipantId.Value
                                )
                            )

                            ||

                            r.UserId == id

                            ||

                            (
                                user.Email != null

                                &&

                                r.UserEmail ==
                                    user.Email
                            )
                    );


            if (hasHistory)
            {
                TempData["Error"] =
                    "Cannot delete: this account is linked to youth participant, registration, attendance, photo, or certificate history. Use 'Deactivate' to preserve the records.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // ------------------------------------------------------
            // SAFE DELETION PATH (PHASE 2)
            //
            // No protected history: remove empty linked participant
            // records together with the account so nothing is
            // orphaned.
            // ------------------------------------------------------

            if (linkedParticipantIds.Any())
            {
                var emptyParticipants =
                    await _context.YouthParticipants
                        .Where(
                            p =>
                                linkedParticipantIds
                                    .Contains(
                                        p.Id
                                    )
                        )
                        .ToListAsync();


                _context.YouthParticipants
                    .RemoveRange(
                        emptyParticipants
                    );


                await _context.SaveChangesAsync();
            }


            var result =
                await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    "Unable to delete the user account.";

                return RedirectToAction(
                    nameof(Index)
                );
            }

            TempData["Success"] =
                "User account deleted successfully.";

            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // RESET PASSWORD - GET
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> ResetPassword(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }


            var user =
                await _userManager.FindByIdAsync(id);


            if (user == null)
            {
                return NotFound();
            }


            var model =
                new ResetPasswordViewModel
                {
                    UserId =
                        user.Id,

                    Email =
                        user.Email
                        ?? user.UserName
                        ?? string.Empty
                };


            return View(model);
        }


        // ==========================================================
        // RESET PASSWORD - POST
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var user =
                await _userManager.FindByIdAsync(
                    model.UserId
                );


            if (user == null)
            {
                return NotFound();
            }


            var token =
                await _userManager.GeneratePasswordResetTokenAsync(
                    user
                );


            var result =
                await _userManager.ResetPasswordAsync(
                    user,
                    token,
                    model.NewPassword
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
                $"Password for '{user.Email}' was successfully reset.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // LOAD ROLES
        // ==========================================================

        private const string ProfileImageClaimType = "BYPManagementSystem.ProfileImagePath";
        private const long MaximumProfileImageBytes = 5 * 1024 * 1024;

        private void ValidateProfileImage(IFormFile? image)
        {
            if (image is not { Length: > 0 }) return;

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };

            if (!allowedExtensions.Contains(extension) ||
                !allowedContentTypes.Contains(image.ContentType.ToLowerInvariant()))
            {
                ModelState.AddModelError(nameof(UserCreateViewModel.ProfileImage),
                    "Upload a JPG, JPEG, PNG, or WEBP image.");
            }
            else if (image.Length > MaximumProfileImageBytes)
            {
                ModelState.AddModelError(nameof(UserCreateViewModel.ProfileImage),
                    "Profile images must be 5 MB or smaller.");
            }
        }

        private async Task<string> SaveProfileImageAsync(IFormFile image)
        {
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var relativePath = $"/uploads/profile-images/{fileName}";
            var directory = Path.Combine(_environment.WebRootPath, "uploads", "profile-images");
            Directory.CreateDirectory(directory);

            await using var stream = new FileStream(
                Path.Combine(directory, fileName), FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await image.CopyToAsync(stream);
            return relativePath;
        }

        private async Task<string?> GetProfileImagePathAsync(IdentityUser user) =>
            (await _userManager.GetClaimsAsync(user))
                .FirstOrDefault(c => c.Type == ProfileImageClaimType)?.Value;

        private void DeleteProfileImage(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) ||
                !relativePath.StartsWith("/uploads/profile-images/", StringComparison.Ordinal)) return;

            var fileName = Path.GetFileName(relativePath);
            var path = Path.Combine(_environment.WebRootPath, "uploads", "profile-images", fileName);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }

        private async Task LoadRolesViewBag()
        {
            ViewBag.Roles =
                await _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .Select(r => r.Name!)
                    .ToListAsync();
        }


        // ==========================================================
        // VIEW MODELS
        // ==========================================================

        public class UserManagementRowViewModel
        {
            public string Id { get; set; }
                = string.Empty;

            public string Email { get; set; }
                = string.Empty;

            public string UserName { get; set; }
                = string.Empty;

            public string Role { get; set; }
                = string.Empty;

            public bool IsActive { get; set; }

            public bool EmailConfirmed { get; set; }

            public DateTimeOffset? LockoutEnd { get; set; }
        }


        public class UserCreateViewModel
        {
            public IFormFile? ProfileImage { get; set; }
            [Required]
            [EmailAddress]
            [StringLength(256)]
            public string Email { get; set; }
                = string.Empty;


            [Required]
            [StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }
                = string.Empty;


            [Required]
            [Compare(
                nameof(Password),
                ErrorMessage = "Passwords do not match."
            )]
            [DataType(DataType.Password)]
            public string ConfirmPassword { get; set; }
                = string.Empty;


            [Required]
            public string Role { get; set; }
                = "User";


            public bool EmailConfirmed { get; set; }
                = true;


            // ------------------------------------------------------
            // OPTIONAL YOUTH INFORMATION
            // ------------------------------------------------------

            [StringLength(100)]
            public string? FirstName { get; set; }


            [StringLength(100)]
            public string? LastName { get; set; }


            [DataType(DataType.Date)]
            public DateTime? BirthDate { get; set; }


            [StringLength(30)]
            public string? ContactNumber { get; set; }


            [StringLength(250)]
            public string? Address { get; set; }


            [StringLength(100)]
            public string? Barangay { get; set; }
        }


        public class UserEditViewModel
        {
            public IFormFile? ProfileImage { get; set; }
            [Required]
            public string Id { get; set; }
                = string.Empty;


            [Required]
            [EmailAddress]
            [StringLength(256)]
            public string Email { get; set; }
                = string.Empty;


            [Required]
            public string Role { get; set; }
                = "User";


            public bool EmailConfirmed { get; set; }


            public bool IsActive { get; set; }


            // ------------------------------------------------------
            // YOUTH INFORMATION
            // ------------------------------------------------------

            [StringLength(100)]
            public string FirstName { get; set; }
                = string.Empty;


            [StringLength(100)]
            public string LastName { get; set; }
                = string.Empty;


            [DataType(DataType.Date)]
            public DateTime? BirthDate { get; set; }


            [StringLength(30)]
            public string? ContactNumber { get; set; }


            [StringLength(250)]
            public string? Address { get; set; }


            [StringLength(100)]
            public string Barangay { get; set; }
                = string.Empty;
        }


        public class ResetPasswordViewModel
        {
            public string UserId { get; set; }
                = string.Empty;


            public string Email { get; set; }
                = string.Empty;


            [Required]
            [DataType(DataType.Password)]
            [StringLength(
                100,
                MinimumLength = 6,
                ErrorMessage =
                    "Password must be at least 6 characters."
            )]
            public string NewPassword { get; set; }
                = string.Empty;


            [Required]
            [Compare(
                nameof(NewPassword),
                ErrorMessage = "Passwords do not match."
            )]
            [DataType(DataType.Password)]
            public string ConfirmPassword { get; set; }
                = string.Empty;
        }
    }
}
