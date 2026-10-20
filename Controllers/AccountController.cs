using BYPManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BYPManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        // ==========================================================
        // DEFAULT ROLE FOR NEW SELF-REGISTERED ACCOUNTS
        // ==========================================================

        private const string DefaultUserRole = "User";


        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }


        // ==========================================================
        // LOGIN - GET
        // ==========================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }


            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };

            return View(model);
        }


        // ==========================================================
        // LOGIN - POST
        // ==========================================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var email = model.Email.Trim();

            model.Email = email;


            var user =
                await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password."
                );

                return View(model);
            }


            var result =
                await _signInManager.PasswordSignInAsync(
                    user,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true
                );


            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.ReturnUrl) &&
                    Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "Home");
            }


            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This account is locked out. Please try again later."
                );

                return View(model);
            }


            ModelState.AddModelError(
                string.Empty,
                "Invalid email or password."
            );

            return View(model);
        }


        // ==========================================================
        // REGISTER - GET
        // ==========================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }


            var model = new RegisterViewModel
            {
                ReturnUrl = returnUrl
            };

            return View(model);
        }


        // ==========================================================
        // REGISTER - POST
        // ==========================================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ClearPasswordFields(model);

                return View(model);
            }


            var email = model.Email.Trim();

            var password = model.Password;

            model.Email = email;


            // ModelState still holds the raw posted values, and the
            // input tag helpers render those first. Keep the safe,
            // trimmed email and clear both password values so a
            // failed POST reflects what the user should see.
            ModelState.SetModelValue(
                nameof(model.Email),
                email,
                email
            );


            IdentityUser? createdUser = null;

            try
            {
                // --------------------------------------------------
                // DUPLICATE EMAIL (friendly field-level message)
                // --------------------------------------------------

                var existingUser =
                    await _userManager.FindByEmailAsync(email);

                if (existingUser != null)
                {
                    ModelState.AddModelError(
                        nameof(model.Email),
                        "An account with this email already exists."
                    );

                    ClearPasswordFields(model);

                    return View(model);
                }


                // --------------------------------------------------
                // CREATE THE ACCOUNT
                // --------------------------------------------------
                // Email confirmation is NOT required by this
                // application (SignIn.RequireConfirmedAccount is
                // false in Program.cs) and no confirmation-token
                // email workflow exists. EmailConfirmed therefore
                // stays false: the account can sign in, but the
                // system never claims an unverified address was
                // confirmed.

                var user = new IdentityUser
                {
                    UserName = email,

                    Email = email,

                    EmailConfirmed = false
                };


                var createResult =
                    await _userManager.CreateAsync(
                        user,
                        password
                    );


                if (!createResult.Succeeded)
                {
                    AddCreateErrors(createResult, model);

                    _logger.LogWarning(
                        "Registration of {Email} failed: {Errors}",
                        email,
                        DescribeErrors(createResult.Errors)
                    );

                    ClearPasswordFields(model);

                    return View(model);
                }


                createdUser = user;


                // --------------------------------------------------
                // ASSIGN DEFAULT "User" ROLE
                //
                // A registration is only complete when the account
                // has its required role. If role assignment fails
                // for any reason, the just-created account is
                // rolled back so the database never keeps an
                // account that cannot access the Youth portal.
                // --------------------------------------------------

                var roleErrors =
                    await AssignDefaultRoleAsync(user, email);

                if (roleErrors != null)
                {
                    await RollBackUserAsync(user, email);

                    createdUser = null;

                    ModelState.AddModelError(
                        string.Empty,
                        roleErrors
                    );

                    ClearPasswordFields(model);

                    return View(model);
                }


                // Registration succeeded. From here on, failures
                // must NOT delete the valid account.
                createdUser = null;


                try
                {
                    await _signInManager.SignInAsync(
                        user,
                        isPersistent: false
                    );
                }
                catch (Exception signInException)
                    when (signInException
                        is not OperationCanceledException)
                {
                    // The account and role exist and are usable.
                    // Report honestly instead of pretending the
                    // user is signed in.
                    _logger.LogError(
                        signInException,
                        "New account {Email} was created, but automatic sign-in failed.",
                        email
                    );

                    TempData["RegistrationMessage"] =
                        "Your account was created successfully, but we could not sign you in automatically. Please sign in with your new credentials.";

                    return RedirectToAction(nameof(Login));
                }


                _logger.LogInformation(
                    "New youth account registered: {Email} (user id {UserId}).",
                    email,
                    user.Id
                );


                TempData["Success"] =
                    "Account created successfully. Complete your youth profile to get started.";


                if (!string.IsNullOrEmpty(model.ReturnUrl) &&
                    Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception exception)
                when (exception is not OperationCanceledException)
            {
                // Database outages and other unexpected server
                // failures are logged and surfaced to the user,
                // never swallowed. If an account was created but
                // the registration could not finish, it is rolled
                // back instead of being left behind half-set-up.
                _logger.LogError(
                    exception,
                    "Unexpected error while registering {Email}.",
                    email
                );


                if (createdUser != null)
                {
                    await RollBackUserAsync(
                        createdUser,
                        email
                    );
                }


                ModelState.AddModelError(
                    string.Empty,
                    "We could not create your account because of an unexpected server error. Please try again. If the problem continues, contact the barangay office."
                );

                ClearPasswordFields(model);

                return View(model);
            }
        }


        // ==========================================================
        // LOGOUT - POST
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login", "Account");
        }


        // ==========================================================
        // ACCESS DENIED
        // ==========================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Staff"))
                {
                    return RedirectToAction("Dashboard", "Staff");
                }

                if (User.IsInRole("User"))
                {
                    return RedirectToAction("Dashboard", "Youth");
                }
            }

            return View();
        }


        // ==========================================================
        // ROLE ASSIGNMENT HELPER
        // ==========================================================
        // Returns a user-facing error message when Role
        // assignment failed or threw, otherwise null.
        // ==========================================================

        private async Task<string?> AssignDefaultRoleAsync(
            IdentityUser user,
            string email)
        {
            const string friendlyMessage =
                "Your account could not be fully set up because the default user role could not be assigned. Please try again. If the problem continues, contact the barangay office.";


            IdentityResult roleResult;

            try
            {
                roleResult =
                    await _userManager.AddToRoleAsync(
                        user,
                        DefaultUserRole
                    );
            }
            catch (Exception roleException)
                when (roleException
                    is not OperationCanceledException)
            {
                _logger.LogError(
                    roleException,
                    "Assigning role {Role} to newly registered user {Email} failed.",
                    DefaultUserRole,
                    email
                );

                return friendlyMessage;
            }


            if (roleResult.Succeeded)
            {
                return null;
            }


            _logger.LogError(
                "Assigning role {Role} to newly registered user {Email} failed: {Errors}",
                DefaultUserRole,
                email,
                DescribeErrors(roleResult.Errors)
            );

            return friendlyMessage;
        }


        // ==========================================================
        // ROLLBACK HELPER
        // ==========================================================
        // Deletes a half-created account and logs every failure
        // instead of ignoring it.
        // ==========================================================

        private async Task RollBackUserAsync(
            IdentityUser user,
            string email)
        {
            try
            {
                var deleteResult =
                    await _userManager.DeleteAsync(user);

                if (deleteResult.Succeeded)
                {
                    _logger.LogInformation(
                        "Rolled back incomplete registration for {Email} (user id {UserId}).",
                        email,
                        user.Id
                    );

                    return;
                }


                _logger.LogCritical(
                    "Registration rollback failed for {Email} (user id {UserId}). The account may be left without the {Role} role. Errors: {Errors}",
                    email,
                    user.Id,
                    DefaultUserRole,
                    DescribeErrors(deleteResult.Errors)
                );
            }
            catch (Exception rollbackException)
            {
                _logger.LogCritical(
                    rollbackException,
                    "Registration rollback failed for {Email} (user id {UserId}). The account may be left without the {Role} role.",
                    email,
                    user.Id,
                    DefaultUserRole
                );
            }
        }


        // ==========================================================
        // HELPERS
        // ==========================================================

        private void ClearPasswordFields(RegisterViewModel model)
        {
            model.Password = string.Empty;
            model.ConfirmPassword = string.Empty;

            // Prevent the input tag helpers from re-rendering the
            // attempted password values after a failed POST.
            ModelState.SetModelValue(
                nameof(model.Password),
                string.Empty,
                string.Empty
            );

            ModelState.SetModelValue(
                nameof(model.ConfirmPassword),
                string.Empty,
                string.Empty
            );
        }


        private void AddCreateErrors(
            IdentityResult result,
            RegisterViewModel model)
        {
            foreach (var error in result.Errors)
            {
                switch (error.Code)
                {
                    case nameof(IdentityErrorDescriber.DuplicateEmail):
                    case nameof(IdentityErrorDescriber.DuplicateUserName):

                        ModelState.AddModelError(
                            nameof(model.Email),
                            "An account with this email already exists."
                        );

                        break;


                    case nameof(IdentityErrorDescriber.PasswordTooShort):

                        ModelState.AddModelError(
                            nameof(model.Password),
                            "Password must be at least 8 characters long."
                        );

                        break;


                    case nameof(IdentityErrorDescriber.PasswordRequiresUpper):

                        ModelState.AddModelError(
                            nameof(model.Password),
                            "Password must include at least one uppercase letter."
                        );

                        break;


                    case nameof(IdentityErrorDescriber.PasswordRequiresLower):

                        ModelState.AddModelError(
                            nameof(model.Password),
                            "Password must include at least one lowercase letter."
                        );

                        break;


                    case nameof(IdentityErrorDescriber.PasswordRequiresDigit):

                        ModelState.AddModelError(
                            nameof(model.Password),
                            "Password must include at least one number."
                        );

                        break;


                    case nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric):

                        ModelState.AddModelError(
                            nameof(model.Password),
                            "Password must include at least one special character."
                        );

                        break;


                    case nameof(IdentityErrorDescriber.InvalidEmail):
                    case nameof(IdentityErrorDescriber.InvalidUserName):

                        ModelState.AddModelError(
                            nameof(model.Email),
                            "Please enter a valid email address."
                        );

                        break;


                    default:

                        ModelState.AddModelError(
                            string.Empty,
                            error.Description
                        );

                        break;
                }
            }
        }


        private static string DescribeErrors(
            IEnumerable<IdentityError> errors)
        {
            return string.Join(
                "; ",
                errors.Select(
                    error =>
                        $"{error.Code}: {error.Description}"
                )
            );
        }
    }
}
