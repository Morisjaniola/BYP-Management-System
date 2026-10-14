using BYPManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BYPManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
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


            var user =
                await _userManager.FindByEmailAsync(model.Email);

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
                return View(model);
            }


            var existingUser =
                await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "An account with this email already exists."
                );

                return View(model);
            }


            var user = new IdentityUser
            {
                UserName = model.Email,

                Email = model.Email,

                EmailConfirmed = true
            };


            var createResult =
                await _userManager.CreateAsync(
                    user,
                    model.Password
                );


            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                return View(model);
            }


            // ------------------------------------------------------
            // ASSIGN DEFAULT "User" ROLE
            //
            // Do not assume role assignment succeeded. If it fails,
            // remove the just-created user so we never leave behind
            // an account that cannot access the Youth portal, and
            // report the problem instead of showing a false success.
            // ======================================================

            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    "User"
                );


            if (!roleResult.Succeeded)
            {
                // Roll back the user so the account is not left
                // half-created without its required role.
                await _userManager.DeleteAsync(user);

                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Your account could not be fully set up. Please try again."
                );

                return View(model);
            }


            await _signInManager.SignInAsync(
                user,
                isPersistent: false
            );


            TempData["Success"] =
                "Account created successfully. Welcome!";


            if (!string.IsNullOrEmpty(model.ReturnUrl) &&
                Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
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
    }
}
