using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BYPManagementSystem.Controllers
{
    // ============================================================
    // ADMIN CONTROLLER
    // ============================================================

    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        // ==========================================================
        // ADMIN DASHBOARD
        // ==========================================================

        [HttpGet]
        public IActionResult Dashboard()
        {
            return View();
        }


        // ==========================================================
        // QUICK ACCESS - YOUTH PARTICIPANTS
        // ==========================================================

        [HttpGet]
        public IActionResult YouthParticipants()
        {
            return RedirectToAction(
                "Index",
                "YouthParticipants"
            );
        }


        // ==========================================================
        // QUICK ACCESS - PROGRAMS
        // ==========================================================

        [HttpGet]
        public IActionResult Programs()
        {
            return RedirectToAction(
                "Index",
                "Programs"
            );
        }


        // ==========================================================
        // QUICK ACCESS - REGISTRATION
        // ==========================================================

        [HttpGet]
        public IActionResult Registration()
        {
            return RedirectToAction(
                "Index",
                "Registration"
            );
        }


        // ==========================================================
        // QUICK ACCESS - ATTENDANCE
        // ==========================================================

        [HttpGet]
        public IActionResult Attendance()
        {
            return RedirectToAction(
                "Index",
                "Attendance"
            );
        }


        // ==========================================================
        // QUICK ACCESS - REPORTS
        // ==========================================================

        [HttpGet]
        public IActionResult Reports()
        {
            return RedirectToAction(
                "Index",
                "Reports"
            );
        }


        // ==========================================================
        // QUICK ACCESS - USER MANAGEMENT
        // ==========================================================

        [HttpGet]
        public IActionResult UserManagement()
        {
            return RedirectToAction(
                "Index",
                "UserManagement"
            );
        }


        // ==========================================================
        // QUICK ACCESS - ROLE MANAGEMENT
        // ==========================================================

        [HttpGet]
        public IActionResult RoleManagement()
        {
            return RedirectToAction(
                "Index",
                "RoleManagement"
            );
        }


        // ==========================================================
        // QUICK ACCESS - PROFILE
        // ==========================================================

        [HttpGet]
        public IActionResult Profile()
        {
            return RedirectToAction(
                "Index",
                "Profile"
            );
        }
    }
}