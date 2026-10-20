using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BYPManagementSystem.Data;
using BYPManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BYPManagementSystem.Controllers
{
    // ============================================================
    // PROFILE CONTROLLER
    // ============================================================

    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWebHostEnvironment _environment;
        // Reused by YouthParticipantsController to read the same
        // claim when rendering the admin pages.
        public const string ProfileImageClaimType = "BYPManagementSystem.ProfileImagePath";
        private const long MaximumProfileImageBytes = 5 * 1024 * 1024;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _environment = environment;
        }


        // ==========================================================
        // PROFILE INPUT MODEL
        // ==========================================================

        public class ProfileInputModel
        {
            public IFormFile? ProfileImage { get; set; }

            public string? ProfileImagePath { get; set; }
            [Required]
            [EmailAddress]
            [StringLength(256)]
            public string Email { get; set; } = string.Empty;

            [StringLength(100)]
            public string FirstName { get; set; } = string.Empty;

            [StringLength(100)]
            public string LastName { get; set; } = string.Empty;

            [DataType(DataType.Date)]
            public DateTime? BirthDate { get; set; }

            [StringLength(30)]
            public string? ContactNumber { get; set; }

            [StringLength(250)]
            public string? Address { get; set; }

            [StringLength(100)]
            public string Barangay { get; set; } = string.Empty;
        }


        // ==========================================================
        // PROFILE - GET
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }


            // ------------------------------------------------------
            // GET CURRENT USER ROLES
            // ------------------------------------------------------

            var roles =
                await _userManager.GetRolesAsync(user);


            var roleName =
                roles.FirstOrDefault()
                ?? "User";


            // ------------------------------------------------------
            // FIND LINKED YOUTH PARTICIPANT
            // ------------------------------------------------------

            var participant =
                await _context.YouthParticipants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        y => y.UserId == user.Id
                    );


            // ------------------------------------------------------
            // BUILD PROFILE MODEL
            // ------------------------------------------------------

            var model = new ProfileInputModel
            {
                Email =
                    user.Email
                    ?? user.UserName
                    ?? string.Empty,

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

                ProfileImagePath = await GetProfileImagePathAsync(user)
            };


            // ------------------------------------------------------
            // VIEW DATA
            // ------------------------------------------------------

            ViewBag.Role =
                roleName;

            ViewBag.UserId =
                user.Id;

            ViewBag.HasYouthProfile =
                participant != null;

            ViewBag.Participant =
                participant;

            ViewBag.CreatedAt =
                participant?.CreatedAt;

            return View(model);
        }


        // ==========================================================
        // PROFILE - UPDATE
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            ProfileInputModel model)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }


            // ------------------------------------------------------
            // GET CURRENT ROLE
            // ------------------------------------------------------

            var roles =
                await _userManager.GetRolesAsync(user);

            var roleName =
                roles.FirstOrDefault()
                ?? "User";

            model.ProfileImagePath = await GetProfileImagePathAsync(user);
            ValidateProfileImage(model.ProfileImage);


            // ------------------------------------------------------
            // BASIC VALIDATION
            // ------------------------------------------------------

            if (!ModelState.IsValid)
            {
                await LoadProfileViewData(
                    user,
                    roleName
                );

                return View(model);
            }


            // ------------------------------------------------------
            // NORMALIZE EMAIL
            // ------------------------------------------------------

            var newEmail =
                model.Email.Trim();


            // ------------------------------------------------------
            // CHECK EMAIL DUPLICATE
            // ------------------------------------------------------

            var existingUser =
                await _userManager.FindByEmailAsync(
                    newEmail
                );

            if (existingUser != null &&
                existingUser.Id != user.Id)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "This email address is already being used."
                );

                await LoadProfileViewData(
                    user,
                    roleName
                );

                return View(model);
            }


            // ------------------------------------------------------
            // UPDATE IDENTITY EMAIL
            // ------------------------------------------------------

            var oldEmail =
                user.Email;

            if (!string.Equals(
                    oldEmail,
                    newEmail,
                    StringComparison.OrdinalIgnoreCase))
            {
                var emailResult =
                    await _userManager.SetEmailAsync(
                        user,
                        newEmail
                    );

                if (!emailResult.Succeeded)
                {
                    foreach (var error in
                        emailResult.Errors)
                    {
                        ModelState.AddModelError(
                            nameof(model.Email),
                            error.Description
                        );
                    }

                    await LoadProfileViewData(
                        user,
                        roleName
                    );

                    return View(model);
                }


                // Keep username synchronized
                var usernameResult =
                    await _userManager.SetUserNameAsync(
                        user,
                        newEmail
                    );

                if (!usernameResult.Succeeded)
                {
                    foreach (var error in
                        usernameResult.Errors)
                    {
                        ModelState.AddModelError(
                            nameof(model.Email),
                            error.Description
                        );
                    }

                    await LoadProfileViewData(
                        user,
                        roleName
                    );

                    return View(model);
                }
            }


            // ------------------------------------------------------
            // USER / YOUTH PROFILE
            // ------------------------------------------------------

            if (User.IsInRole("User"))
            {
                // --------------------------------------------------
                // VALIDATE REQUIRED YOUTH FIELDS
                // --------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                        model.FirstName))
                {
                    ModelState.AddModelError(
                        nameof(model.FirstName),
                        "First name is required."
                    );
                }


                if (string.IsNullOrWhiteSpace(
                        model.LastName))
                {
                    ModelState.AddModelError(
                        nameof(model.LastName),
                        "Last name is required."
                    );
                }


                if (string.IsNullOrWhiteSpace(
                        model.Barangay))
                {
                    ModelState.AddModelError(
                        nameof(model.Barangay),
                        "Barangay is required."
                    );
                }


                if (!ModelState.IsValid)
                {
                    await LoadProfileViewData(
                        user,
                        roleName
                    );

                    return View(model);
                }


                // --------------------------------------------------
                // GET OR CREATE YOUTH PROFILE
                // --------------------------------------------------

                var participant =
                    await _context.YouthParticipants
                        .FirstOrDefaultAsync(
                            y => y.UserId == user.Id
                        );


                if (participant == null)
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
                else
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
                }
            }

            string? newImagePath = null;
            if (model.ProfileImage is { Length: > 0 })
            {
                newImagePath = await SaveProfileImageAsync(model.ProfileImage);
                var previousImagePath = model.ProfileImagePath;
                var imageClaimResult = string.IsNullOrWhiteSpace(previousImagePath)
                    ? await _userManager.AddClaimAsync(user,
                        new Claim(ProfileImageClaimType, newImagePath))
                    : await _userManager.ReplaceClaimAsync(user,
                        new Claim(ProfileImageClaimType, previousImagePath),
                        new Claim(ProfileImageClaimType, newImagePath));

                if (!imageClaimResult.Succeeded)
                {
                    DeleteProfileImage(newImagePath);
                    foreach (var error in imageClaimResult.Errors)
                    {
                        ModelState.AddModelError(nameof(model.ProfileImage), error.Description);
                    }
                    await LoadProfileViewData(user, roleName);
                    return View(model);
                }

                DeleteProfileImage(previousImagePath);
                model.ProfileImagePath = newImagePath;
            }


            // ------------------------------------------------------
            // SAVE IDENTITY + PROFILE CHANGES
            // ------------------------------------------------------

            try
            {
                // Update registration email references
                // for the current account.
                var registrations =
                    await _context.ProgramRegistrations
                        .Where(
                            r =>
                                r.UserId == user.Id
                        )
                        .ToListAsync();


                foreach (var registration
                    in registrations)
                {
                    registration.UserEmail =
                        newEmail;
                }


                await _context.SaveChangesAsync();


                TempData["Success"] =
                    "Your profile has been successfully updated.";

                return RedirectToAction(
                    nameof(Index)
                );
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The profile could not be saved. Please try again."
                );

                await LoadProfileViewData(
                    user,
                    roleName
                );

                return View(model);
            }
        }


        // ==========================================================
        // CHANGE PASSWORD - GET
        // ==========================================================
        //
        // Self-service password change. The user must supply their
        // CURRENT password; only the signed-in user's own password
        // can be changed.
        // ==========================================================

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(
                new ChangePasswordViewModel()
            );
        }


        // ==========================================================
        // CHANGE PASSWORD - POST
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }


            // ------------------------------------------------------
            // VERIFY THE CURRENT PASSWORD
            //
            // Never change a password without proving knowledge of
            // the existing one.
            // ======================================================

            var currentPasswordOk =
                await _userManager.CheckPasswordAsync(
                    user,
                    model.CurrentPassword
                );

            if (!currentPasswordOk)
            {
                ModelState.AddModelError(
                    nameof(model.CurrentPassword),
                    "The current password is incorrect."
                );

                return View(model);
            }


            // ------------------------------------------------------
            // APPLY THE NEW PASSWORD
            // ======================================================

            var changeResult =
                await _userManager.ChangePasswordAsync(
                    user,
                    model.CurrentPassword,
                    model.NewPassword
                );

            if (!changeResult.Succeeded)
            {
                foreach (var error in
                    changeResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                return View(model);
            }


            // ------------------------------------------------------
            // REFRESH THE SIGN-IN COOKIE
            //
            // Changing the password updates the security stamp, so
            // re-sign the user in to keep the session valid with
            // the new credentials.
            // ======================================================

            await _signInManager.RefreshSignInAsync(
                user
            );


            TempData["Success"] =
                "Your password has been changed successfully.";


            return RedirectToAction(nameof(Index));
        }


        // ==========================================================
        // LOAD VIEW DATA
        // ==========================================================

        private async Task LoadProfileViewData(
            IdentityUser user,
            string roleName)
        {
            var participant =
                await _context.YouthParticipants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        y => y.UserId == user.Id
                    );


            ViewBag.Role =
                roleName;

            ViewBag.UserId =
                user.Id;

            ViewBag.HasYouthProfile =
                participant != null;

            ViewBag.Participant =
                participant;

            ViewBag.CreatedAt =
                participant?.CreatedAt;
        }

        private void ValidateProfileImage(IFormFile? image)
        {
            if (image is not { Length: > 0 }) return;

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var validContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!validExtensions.Contains(extension) ||
                !validContentTypes.Contains(image.ContentType.ToLowerInvariant()))
                ModelState.AddModelError(nameof(ProfileInputModel.ProfileImage),
                    "Upload a JPG, JPEG, PNG, or WEBP image.");
            else if (image.Length > MaximumProfileImageBytes)
                ModelState.AddModelError(nameof(ProfileInputModel.ProfileImage),
                    "Profile images must be 5 MB or smaller.");
            else if (!HasValidImageSignature(image))
                ModelState.AddModelError(nameof(ProfileInputModel.ProfileImage),
                    "Upload a JPG, JPEG, PNG, or WEBP image.");
        }


        // ==========================================================
        // IMAGE CONTENT SIGNATURE CHECK
        //
        // Extension and Content-Type are client-controlled and can be
        // spoofed. Verify the file actually starts with a supported
        // image header (JPEG / PNG / WEBP) before it is stored.
        // ==========================================================

        private static bool HasValidImageSignature(IFormFile image)
        {
            using var stream = image.OpenReadStream();

            var header = new byte[12];
            var read = 0;
            while (read < header.Length)
            {
                var count = stream.Read(header, read, header.Length - read);
                if (count == 0) break;
                read += count;
            }

            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            // JPEG: FF D8 FF
            if (read >= 3 &&
                header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            {
                return true;
            }

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (read >= 8 &&
                header[0] == 0x89 && header[1] == 0x50 &&
                header[2] == 0x4E && header[3] == 0x47 &&
                header[4] == 0x0D && header[5] == 0x0A &&
                header[6] == 0x1A && header[7] == 0x0A)
            {
                return true;
            }

            // WEBP: "RIFF" .... "WEBP"
            if (read >= 12 &&
                header[0] == (byte)'R' && header[1] == (byte)'I' &&
                header[2] == (byte)'F' && header[3] == (byte)'F' &&
                header[8] == (byte)'W' && header[9] == (byte)'E' &&
                header[10] == (byte)'B' && header[11] == (byte)'P')
            {
                return true;
            }

            return false;
        }

        private async Task<string?> GetProfileImagePathAsync(IdentityUser user) =>
            (await _userManager.GetClaimsAsync(user))
                .FirstOrDefault(c => c.Type == ProfileImageClaimType)?.Value;

        private async Task<string> SaveProfileImageAsync(IFormFile image)
        {
            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(image.FileName).ToLowerInvariant()}";
            var directory = Path.Combine(_environment.WebRootPath, "uploads", "profile-images");
            Directory.CreateDirectory(directory);
            await using var stream = new FileStream(Path.Combine(directory, fileName),
                FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await image.CopyToAsync(stream);
            return $"/uploads/profile-images/{fileName}";
        }

        private void DeleteProfileImage(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) ||
                !relativePath.StartsWith("/uploads/profile-images/", StringComparison.Ordinal)) return;
            var fileName = Path.GetFileName(relativePath);
            var path = Path.Combine(_environment.WebRootPath, "uploads", "profile-images", fileName);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }
}
