using System.Security.Claims;
using BYPManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BYPManagementSystem.Controllers
{
    // ============================================================
    // YOUTH / USER CONTROLLER
    // ============================================================

    [Authorize(Roles = "User")]
    public class YouthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public YouthController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // ==========================================================
        // YOUTH DASHBOARD
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            // ------------------------------------------------------
            // GET CURRENT USER INFORMATION
            // ------------------------------------------------------

            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            var userEmail =
                User.Identity?.Name;


            // ------------------------------------------------------
            // FIND LINKED YOUTH PARTICIPANT
            // ------------------------------------------------------

            var participant =
                await _context.YouthParticipants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        y =>
                            (
                                userId != null &&
                                y.UserId == userId
                            )
                            ||
                            (
                                !string.IsNullOrWhiteSpace(userEmail) &&
                                _context.ProgramRegistrations
                                    .Any(
                                        r =>
                                            r.YouthParticipantId == y.Id &&
                                            r.UserEmail == userEmail
                                    )
                            )
                    );


            // ------------------------------------------------------
            // REGISTRATIONS
            // ------------------------------------------------------

            var registrationsQuery =
                _context.ProgramRegistrations
                    .AsNoTracking()
                    .Include(r => r.Program)
                    .Include(r => r.YouthParticipant)
                    .AsQueryable();


            if (participant != null)
            {
                registrationsQuery =
                    registrationsQuery.Where(
                        r =>
                            r.YouthParticipantId ==
                            participant.Id
                    );
            }
            else if (!string.IsNullOrWhiteSpace(userId))
            {
                registrationsQuery =
                    registrationsQuery.Where(
                        r => r.UserId == userId
                    );
            }
            else if (!string.IsNullOrWhiteSpace(userEmail))
            {
                registrationsQuery =
                    registrationsQuery.Where(
                        r => r.UserEmail == userEmail
                    );
            }
            else
            {
                registrationsQuery =
                    registrationsQuery.Where(
                        r => false
                    );
            }


            var registrations =
                await registrationsQuery
                    .OrderByDescending(
                        r => r.RegisteredAt
                    )
                    .ToListAsync();


            // ------------------------------------------------------
            // REGISTRATION COUNTS
            // ------------------------------------------------------

            var totalRegistrations =
                registrations.Count;


            var pendingRegistrations =
                registrations.Count(
                    r => r.Status == "Pending"
                );


            var approvedRegistrations =
                registrations.Count(
                    r => r.Status == "Approved"
                );


            var rejectedRegistrations =
                registrations.Count(
                    r => r.Status == "Rejected"
                );


            // ------------------------------------------------------
            // ATTENDANCE
            // ------------------------------------------------------

            var registrationIds =
                registrations
                    .Select(r => r.Id)
                    .ToList();


            var attendance =
                await _context.AttendanceRecords
                    .AsNoTracking()
                    .Include(a => a.Registration)
                        .ThenInclude(
                            r => r!.Program
                        )
                    .Where(
                        a =>
                            registrationIds.Contains(
                                a.RegistrationId
                            )
                    )
                    .OrderByDescending(
                        a => a.AttendanceDate
                    )
                    .ThenByDescending(
                        a => a.RecordedAt
                    )
                    .ToListAsync();


            // ------------------------------------------------------
            // ATTENDANCE COUNTS
            // ------------------------------------------------------

            var totalAttendance =
                attendance.Count;


            var presentCount =
                attendance.Count(
                    a => a.Status == "Present"
                );


            var absentCount =
                attendance.Count(
                    a => a.Status == "Absent"
                );


            var lateCount =
                attendance.Count(
                    a => a.Status == "Late"
                );


            var excusedCount =
                attendance.Count(
                    a => a.Status == "Excused"
                );


            // ------------------------------------------------------
            // AVAILABLE PROGRAMS
            // ------------------------------------------------------
            // User / Youth should be able to browse active programs.

            var availablePrograms =
                await _context.YouthPrograms
                    .AsNoTracking()
                    .Where(
                        p => p.Status == "Active"
                    )
                    .OrderByDescending(
                        p => p.CreatedAt
                    )
                    .Take(6)
                    .ToListAsync();


            // ------------------------------------------------------
            // PASS DATA TO VIEW
            // ------------------------------------------------------

            ViewBag.Participant =
                participant;

            ViewBag.UserEmail =
                userEmail
                ?? "Youth User";


            ViewBag.UserId =
                userId;


            ViewBag.TotalRegistrations =
                totalRegistrations;


            ViewBag.PendingRegistrations =
                pendingRegistrations;


            ViewBag.ApprovedRegistrations =
                approvedRegistrations;


            ViewBag.RejectedRegistrations =
                rejectedRegistrations;


            ViewBag.TotalAttendance =
                totalAttendance;


            ViewBag.PresentCount =
                presentCount;


            ViewBag.AbsentCount =
                absentCount;


            ViewBag.LateCount =
                lateCount;


            ViewBag.ExcusedCount =
                excusedCount;


            ViewBag.Registrations =
                registrations;


            ViewBag.Attendance =
                attendance;


            ViewBag.AvailablePrograms =
                availablePrograms;


            // ------------------------------------------------------
            // DASHBOARD DISPLAY NAME
            // ------------------------------------------------------

            ViewBag.DisplayName =
                participant != null
                    ? participant.FullName
                    : (
                        !string.IsNullOrWhiteSpace(userEmail)
                            ? userEmail
                            : "Youth User"
                    );


            return View();
        }


        // ==========================================================
        // MY ATTENDANCE
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> MyAttendance()
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );


            var userEmail =
                User.Identity?.Name;


            // ------------------------------------------------------
            // FIND LINKED YOUTH PARTICIPANT
            // ------------------------------------------------------

            var participant =
                await _context.YouthParticipants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        y =>
                            (
                                userId != null &&
                                y.UserId == userId
                            )
                            ||
                            (
                                !string.IsNullOrWhiteSpace(userEmail) &&
                                _context.ProgramRegistrations
                                    .Any(
                                        r =>
                                            r.YouthParticipantId == y.Id &&
                                            r.UserEmail == userEmail
                                    )
                            )
                    );


            // ------------------------------------------------------
            // GET REGISTRATION IDS FOR THIS USER
            // ------------------------------------------------------

            // Get registration IDs for this user
            var registrationIds =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .Where(r =>
                        (userId != null && r.UserId == userId)
                        ||
                        (participant != null &&
                         r.YouthParticipantId == participant.Id)
                        ||
                        (participant != null &&
                         r.YouthParticipant != null &&
                         r.YouthParticipant.UserId == userId)
                        ||
                        (!string.IsNullOrWhiteSpace(userEmail) &&
                         r.UserEmail == userEmail))
                    .Select(r => r.Id)
                    .ToListAsync();


            // ------------------------------------------------------
            // GET ATTENDANCE RECORDS
            // ------------------------------------------------------

            var records =
                await _context.AttendanceRecords
                    .AsNoTracking()
                    .Include(a => a.Registration)
                        .ThenInclude(r => r!.Program)
                    .Include(a => a.Registration)
                        .ThenInclude(r => r!.YouthParticipant)
                    .Where(
                        a =>
                            registrationIds.Contains(
                                a.RegistrationId
                            )
                    )
                    .OrderByDescending(
                        a => a.AttendanceDate
                    )
                    .ThenByDescending(
                        a => a.RecordedAt
                    )
                    .ToListAsync();


            // ------------------------------------------------------
            // VIEW DATA
            // ------------------------------------------------------

            ViewBag.Participant =
                participant;

            ViewBag.UserId =
                userId;

            ViewBag.AttendanceRecords =
                records;

            ViewBag.HasAttendance =
                records.Any();


            return View(records);
        }
    }
}