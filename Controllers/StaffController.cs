using BYPManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BYPManagementSystem.Controllers
{
    // ============================================================
    // STAFF CONTROLLER
    // ============================================================

    [Authorize(Roles = "Staff")]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StaffController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // ==========================================================
        // STAFF DASHBOARD
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            // ------------------------------------------------------
            // BASIC OPERATIONAL COUNTS
            // ------------------------------------------------------

            var totalParticipants =
                await _context.YouthParticipants
                    .AsNoTracking()
                    .CountAsync();


            var activeParticipants =
                await _context.YouthParticipants
                    .AsNoTracking()
                    .CountAsync(
                        y => y.IsActive
                    );


            var totalPrograms =
                await _context.YouthPrograms
                    .AsNoTracking()
                    .CountAsync();


            var activePrograms =
                await _context.YouthPrograms
                    .AsNoTracking()
                    .CountAsync(
                        p => p.Status == "Active"
                    );


            var totalRegistrations =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .CountAsync();


            var pendingRegistrations =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .CountAsync(
                        r => r.Status == "Pending"
                    );


            var approvedRegistrations =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .CountAsync(
                        r => r.Status == "Approved"
                    );


            var totalAttendance =
                await _context.AttendanceRecords
                    .AsNoTracking()
                    .CountAsync();


            var presentAttendance =
                await _context.AttendanceRecords
                    .AsNoTracking()
                    .CountAsync(
                        a => a.Status == "Present"
                    );


            var absentAttendance =
                await _context.AttendanceRecords
                    .AsNoTracking()
                    .CountAsync(
                        a => a.Status == "Absent"
                    );


            var lateAttendance =
                await _context.AttendanceRecords
                    .AsNoTracking()
                    .CountAsync(
                        a => a.Status == "Late"
                    );


            var excusedAttendance =
                await _context.AttendanceRecords
                    .AsNoTracking()
                    .CountAsync(
                        a => a.Status == "Excused"
                    );


            // ------------------------------------------------------
            // PASS DATA TO DASHBOARD
            // ------------------------------------------------------

            ViewBag.TotalParticipants =
                totalParticipants;

            ViewBag.ActiveParticipants =
                activeParticipants;

            ViewBag.TotalPrograms =
                totalPrograms;

            ViewBag.ActivePrograms =
                activePrograms;

            ViewBag.TotalRegistrations =
                totalRegistrations;

            ViewBag.PendingRegistrations =
                pendingRegistrations;

            ViewBag.ApprovedRegistrations =
                approvedRegistrations;

            ViewBag.TotalAttendance =
                totalAttendance;

            ViewBag.PresentAttendance =
                presentAttendance;

            ViewBag.AbsentAttendance =
                absentAttendance;

            ViewBag.LateAttendance =
                lateAttendance;

            ViewBag.ExcusedAttendance =
                excusedAttendance;


            // ------------------------------------------------------
            // RECENT REGISTRATIONS
            // ------------------------------------------------------

            var recentRegistrations =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .Include(r => r.Program)
                    .Include(r => r.YouthParticipant)
                    .OrderByDescending(
                        r => r.RegisteredAt
                    )
                    .Take(5)
                    .ToListAsync();


            ViewBag.RecentRegistrations =
                recentRegistrations;


            // ------------------------------------------------------
            // RECENT ATTENDANCE
            // ------------------------------------------------------

            var recentAttendance =
                await _context.AttendanceRecords
                    .AsNoTracking()
                    .Include(a => a.Registration)
                        .ThenInclude(
                            r => r!.Program
                        )
                    .Include(a => a.Registration)
                        .ThenInclude(
                            r => r!.YouthParticipant
                        )
                    .OrderByDescending(
                        a => a.AttendanceDate
                    )
                    .ThenByDescending(
                        a => a.RecordedAt
                    )
                    .Take(5)
                    .ToListAsync();


            ViewBag.RecentAttendance =
                recentAttendance;


            // ------------------------------------------------------
            // CURRENT USER
            // ------------------------------------------------------

            ViewBag.StaffEmail =
                User.Identity?.Name
                ?? "Staff";


            ViewBag.Role =
                "Staff";


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