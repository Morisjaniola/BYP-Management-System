using System.Diagnostics;
using BYPManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BYPManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        // =========================================================
        // HOME
        // =========================================================
        //
        // Routes the authenticated user to the dashboard that
        // matches their role. Previously this always redirected to
        // the Admin dashboard, which sent Staff and Youth users to
        // an Access Denied page after login.
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }

                if (User.IsInRole("Staff"))
                {
                    return RedirectToAction("Dashboard", "Staff");
                }

                if (User.IsInRole("User"))
                {
                    return RedirectToAction("Dashboard", "Youth");
                }

                // --------------------------------------------------
                // AUTHENTICATED BUT NO RECOGNIZED ROLE
                //
                // Send to Access Denied instead of looping between
                // Login (which redirects authenticated users back
                // to Index) and Index.
                // --------------------------------------------------

                return RedirectToAction("AccessDenied", "Account");
            }

            // Anonymous users go straight to login.
            return RedirectToAction("Login", "Account");
        }


        // =========================================================
        // PRIVACY
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }


        // =========================================================
        // ERROR
        // =========================================================
        //
        // This action is used by Program.cs:
        //
        // app.UseExceptionHandler("/Home/Error");
        //
        // It returns a friendly error page and a reference ID.
        // =========================================================

        [AllowAnonymous]
        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true
        )]
        public IActionResult Error()
        {
            var model = new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            };

            return View(model);
        }


        // =========================================================
        // STATUS CODE
        // =========================================================
        //
        // Handles common HTTP status codes such as:
        //
        // 401 = Unauthorized
        // 403 = Forbidden
        // 404 = Not Found
        // =========================================================

        // Accepts every verb: UseStatusCodePagesWithReExecute
        // re-executes the ORIGINAL request (including POST form
        // submissions) against this action, so restricting it to
        // GET would replace the real error status (400/404/...)
        // with a misleading 405 from route matching.
        [AllowAnonymous]
        [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
        public new IActionResult StatusCode(int code)
        {
            ViewBag.StatusCode = code;

            ViewBag.Message = code switch
            {
                401 =>
                    "You must sign in to continue.",

                403 =>
                    "You do not have permission to access this page.",

                404 =>
                    "The requested page could not be found.",

                400 =>
                    "The request could not be processed.",

                500 =>
                    "The system encountered an unexpected error.",

                _ =>
                    "An unexpected request error occurred."
            };

            return View();
        }
    }
}