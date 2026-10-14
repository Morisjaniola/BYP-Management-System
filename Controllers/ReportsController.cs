using BYPManagementSystem.Data;
using BYPManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BYPManagementSystem.Controllers
{
    // ============================================================
    // REPORTS
    // Admin  -> System-wide reports
    // Staff  -> Operational reports
    // User   -> Personal reports
    // ============================================================

    [Authorize(Roles = "Admin,Staff,User")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ReportsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // ==========================================================
        // REPORTS INDEX
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? pSearch,
            string? pStatus,
            string? progSearch,
            string? progStatus,
            string? regSearch,
            string? regStatus,
            string? attSearch,
            string? attStatus)
        {
            // ------------------------------------------------------
            // CREATE EMPTY VIEW MODEL
            // ------------------------------------------------------

            var model = new ReportsDashboardViewModel();


            // ------------------------------------------------------
            // CHECK CURRENT USER ROLE
            // ------------------------------------------------------

            var isUser = User.IsInRole("User");

            var isAdmin = User.IsInRole("Admin");

            var isStaff = User.IsInRole("Staff");


            // ======================================================
            // USER / YOUTH PERSONAL REPORT
            // ======================================================

            if (isUser && !isAdmin && !isStaff)
            {
                var userId =
                    _userManager.GetUserId(User);

                var userEmail =
                    User.Identity?.Name;


                // --------------------------------------------------
                // PERSONAL REGISTRATIONS
                // --------------------------------------------------

                var registrationsQuery =
                    _context.ProgramRegistrations
                        .AsNoTracking()
                        .Include(r => r.Program)
                        .Include(r => r.YouthParticipant)
                        .AsQueryable();


                if (!string.IsNullOrWhiteSpace(userId))
                {
                    registrationsQuery =
                        registrationsQuery.Where(r =>
                            r.UserId == userId ||
                            (
                                r.UserId == null &&
                                r.UserEmail == userEmail
                            )
                        );
                }
                else if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    registrationsQuery =
                        registrationsQuery.Where(r =>
                            r.UserEmail == userEmail
                        );
                }
                else
                {
                    registrationsQuery =
                        registrationsQuery.Where(r => false);
                }


                var personalRegistrations =
                    await registrationsQuery
                        .OrderByDescending(r => r.RegisteredAt)
                        .ToListAsync();


                // --------------------------------------------------
                // PERSONAL ATTENDANCE
                // --------------------------------------------------

                var attendanceQuery =
                    _context.AttendanceRecords
                        .AsNoTracking()
                        .Include(a => a.Registration)
                            .ThenInclude(r => r!.Program)
                        .Include(a => a.Registration)
                            .ThenInclude(r => r!.YouthParticipant)
                        .AsQueryable();


                if (!string.IsNullOrWhiteSpace(userId))
                {
                    attendanceQuery =
                        attendanceQuery.Where(a =>
                            a.Registration!.UserId == userId ||
                            (
                                a.Registration!.UserId == null &&
                                a.Registration.UserEmail == userEmail
                            )
                        );
                }
                else if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    attendanceQuery =
                        attendanceQuery.Where(a =>
                            a.Registration!.UserEmail == userEmail
                        );
                }
                else
                {
                    attendanceQuery =
                        attendanceQuery.Where(a => false);
                }


                var personalAttendance =
                    await attendanceQuery
                        .OrderByDescending(a => a.AttendanceDate)
                        .ThenByDescending(a => a.RecordedAt)
                        .ToListAsync();


                // --------------------------------------------------
                // PERSONAL PARTICIPANT RECORD
                // --------------------------------------------------

                var personalParticipant = null
                    as YouthParticipant;


                if (!string.IsNullOrWhiteSpace(userId))
                {
                    personalParticipant =
                        await _context.YouthParticipants
                            .AsNoTracking()
                            .FirstOrDefaultAsync(y =>
                                y.UserId == userId
                            );
                }


                // --------------------------------------------------
                // PERSONAL COUNTS
                // --------------------------------------------------

                model.TotalRegistrations =
                    personalRegistrations.Count;

                model.PendingRegistrations =
                    personalRegistrations.Count(
                        r => r.Status == "Pending"
                    );

                model.ApprovedRegistrations =
                    personalRegistrations.Count(
                        r => r.Status == "Approved"
                    );

                model.RejectedRegistrations =
                    personalRegistrations.Count(
                        r => r.Status == "Rejected"
                    );


                model.TotalAttendanceRecords =
                    personalAttendance.Count;

                model.PresentCount =
                    personalAttendance.Count(
                        a => a.Status == "Present"
                    );

                model.AbsentCount =
                    personalAttendance.Count(
                        a => a.Status == "Absent"
                    );

                model.LateCount =
                    personalAttendance.Count(
                        a => a.Status == "Late"
                    );

                model.ExcusedCount =
                    personalAttendance.Count(
                        a => a.Status == "Excused"
                    );


                // --------------------------------------------------
                // PERSONAL PROGRAM LIST
                // --------------------------------------------------

                model.Programs =
                    personalRegistrations
                        .Where(r => r.Program != null)
                        .Select(r => r.Program!)
                        .GroupBy(p => p.Id)
                        .Select(g => g.First())
                        .OrderByDescending(
                            p => p.Status == "Active"
                        )
                        .ThenBy(p => p.Name)
                        .ToList();


                // --------------------------------------------------
                // PERSONAL REGISTRATION LIST
                // --------------------------------------------------

                model.Registrations =
                    personalRegistrations;


                // --------------------------------------------------
                // PERSONAL ATTENDANCE LIST
                // --------------------------------------------------

                model.AttendanceRecords =
                    personalAttendance;


                // --------------------------------------------------
                // PERSONAL PARTICIPANT LIST
                // --------------------------------------------------

                if (personalParticipant != null)
                {
                    model.YouthParticipants =
                        new List<YouthParticipant>
                        {
                            personalParticipant
                        };
                }


                // --------------------------------------------------
                // REPORT FILTERS (applied after the counts above so
                // the summary tiles keep the full data set)
                // --------------------------------------------------

                model.YouthParticipants =
                    FilterParticipants(
                        model.YouthParticipants,
                        pSearch,
                        pStatus
                    );

                model.Programs =
                    FilterPrograms(
                        model.Programs,
                        progSearch,
                        progStatus
                    );

                model.Registrations =
                    FilterRegistrations(
                        model.Registrations,
                        regSearch,
                        regStatus
                    );

                model.AttendanceRecords =
                    FilterAttendance(
                        model.AttendanceRecords,
                        attSearch,
                        attStatus
                    );


                // --------------------------------------------------
                // USER REPORT
                // --------------------------------------------------

                return View(model);
            }


            // ======================================================
            // ADMIN / STAFF OPERATIONAL REPORT
            // ======================================================

            // ------------------------------------------------------
            // YOUTH PARTICIPANTS
            // ------------------------------------------------------

            model.YouthParticipants =
                await _context.YouthParticipants
                    .AsNoTracking()
                    .OrderBy(y => y.LastName)
                    .ThenBy(y => y.FirstName)
                    .ToListAsync();


            model.TotalParticipants =
                model.YouthParticipants.Count;

            model.ActiveParticipants =
                model.YouthParticipants.Count(
                    y => y.IsActive
                );


            // ------------------------------------------------------
            // PROGRAMS
            // ------------------------------------------------------

            model.Programs =
                await _context.YouthPrograms
                    .AsNoTracking()
                    .OrderByDescending(
                        p => p.Status == "Active"
                    )
                    .ThenBy(p => p.Name)
                    .ToListAsync();


            model.TotalPrograms =
                model.Programs.Count;

            model.ActivePrograms =
                model.Programs.Count(
                    p => p.Status == "Active"
                );


            // ------------------------------------------------------
            // REGISTRATIONS
            // ------------------------------------------------------

            model.Registrations =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .Include(r => r.Program)
                    .Include(r => r.YouthParticipant)
                    .OrderByDescending(
                        r => r.RegisteredAt
                    )
                    .ToListAsync();


            model.TotalRegistrations =
                model.Registrations.Count;

            model.PendingRegistrations =
                model.Registrations.Count(
                    r => r.Status == "Pending"
                );

            model.ApprovedRegistrations =
                model.Registrations.Count(
                    r => r.Status == "Approved"
                );

            model.RejectedRegistrations =
                model.Registrations.Count(
                    r => r.Status == "Rejected"
                );


            // ------------------------------------------------------
            // ATTENDANCE
            // ------------------------------------------------------

            model.AttendanceRecords =
                await _context.AttendanceRecords
                    .AsNoTracking()
                    .Include(a => a.Registration)
                        .ThenInclude(r => r!.Program)
                    .Include(a => a.Registration)
                        .ThenInclude(r => r!.YouthParticipant)
                    .OrderByDescending(
                        a => a.AttendanceDate
                    )
                    .ThenByDescending(
                        a => a.RecordedAt
                    )
                    .ToListAsync();


            model.TotalAttendanceRecords =
                model.AttendanceRecords.Count;

            model.PresentCount =
                model.AttendanceRecords.Count(
                    a => a.Status == "Present"
                );

            model.AbsentCount =
                model.AttendanceRecords.Count(
                    a => a.Status == "Absent"
                );

            model.LateCount =
                model.AttendanceRecords.Count(
                    a => a.Status == "Late"
                );

            model.ExcusedCount =
                model.AttendanceRecords.Count(
                    a => a.Status == "Excused"
                );


            // ------------------------------------------------------
            // REPORT FILTERS (applied after the counts above so
            // the summary tiles keep the full data set)
            // ------------------------------------------------------

            model.YouthParticipants =
                FilterParticipants(
                    model.YouthParticipants,
                    pSearch,
                    pStatus
                );

            model.Programs =
                FilterPrograms(
                    model.Programs,
                    progSearch,
                    progStatus
                );

            model.Registrations =
                FilterRegistrations(
                    model.Registrations,
                    regSearch,
                    regStatus
                );

            model.AttendanceRecords =
                FilterAttendance(
                    model.AttendanceRecords,
                    attSearch,
                    attStatus
                );


            // ------------------------------------------------------
            // RETURN OPERATIONAL REPORT
            // ------------------------------------------------------

            return View(model);
        }


        // ==========================================================
        // REPORT FILTER HELPERS
        // Applied in-memory after loading so the summary tiles
        // always reflect the complete data set.
        // ==========================================================

        private static List<YouthParticipant> FilterParticipants(
            List<YouthParticipant>? items,
            string? search,
            string? status)
        {
            var result = items ?? new List<YouthParticipant>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term =
                    search.Trim().ToLowerInvariant();

                result = result.Where(y =>
                    (y.FirstName + " " + y.LastName)
                        .ToLowerInvariant()
                        .Contains(term)

                    ||

                    (y.Barangay != null &&
                        y.Barangay
                            .ToLowerInvariant()
                            .Contains(term))
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                status != "All")
            {
                var wanted =
                    status == "Active";

                result = result.Where(y =>
                    y.IsActive == wanted
                ).ToList();
            }

            return result;
        }


        private static List<YouthProgram> FilterPrograms(
            List<YouthProgram>? items,
            string? search,
            string? status)
        {
            var result = items ?? new List<YouthProgram>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term =
                    search.Trim().ToLowerInvariant();

                result = result.Where(p =>
                    (p.Name != null &&
                        p.Name
                            .ToLowerInvariant()
                            .Contains(term))

                    ||

                    (p.Description != null &&
                        p.Description
                            .ToLowerInvariant()
                            .Contains(term))

                    ||

                    (p.Schedule != null &&
                        p.Schedule
                            .ToLowerInvariant()
                            .Contains(term))
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                status != "All")
            {
                result = result.Where(p =>
                    p.Status == status
                ).ToList();
            }

            return result;
        }


        private static List<ProgramRegistration> FilterRegistrations(
            List<ProgramRegistration>? items,
            string? search,
            string? status)
        {
            var result =
                items ?? new List<ProgramRegistration>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term =
                    search.Trim().ToLowerInvariant();

                result = result.Where(r =>
                    (r.Program != null &&
                        r.Program.Name != null &&
                        r.Program.Name
                            .ToLowerInvariant()
                            .Contains(term))

                    ||

                    (r.YouthParticipant != null &&
                        (r.YouthParticipant.FirstName +
                            " " +
                            r.YouthParticipant.LastName)
                            .ToLowerInvariant()
                            .Contains(term))

                    ||

                    (r.UserEmail != null &&
                        r.UserEmail
                            .ToLowerInvariant()
                            .Contains(term))

                    ||

                    (r.Status != null &&
                        r.Status
                            .ToLowerInvariant()
                            .Contains(term))
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                status != "All")
            {
                result = result.Where(r =>
                    r.Status == status
                ).ToList();
            }

            return result;
        }


        private static List<AttendanceRecord> FilterAttendance(
            List<AttendanceRecord>? items,
            string? search,
            string? status)
        {
            var result =
                items ?? new List<AttendanceRecord>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term =
                    search.Trim().ToLowerInvariant();

                result = result.Where(a =>
                    (a.Registration != null &&
                        a.Registration.Program != null &&
                        a.Registration.Program.Name != null &&
                        a.Registration.Program.Name
                            .ToLowerInvariant()
                            .Contains(term))

                    ||

                    (a.Registration != null &&
                        a.Registration.YouthParticipant != null &&
                        (a.Registration.YouthParticipant.FirstName +
                            " " +
                            a.Registration.YouthParticipant.LastName)
                            .ToLowerInvariant()
                            .Contains(term))

                    ||

                    (a.Status != null &&
                        a.Status
                            .ToLowerInvariant()
                            .Contains(term))

                    ||

                    (a.Notes != null &&
                        a.Notes
                            .ToLowerInvariant()
                            .Contains(term))
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                status != "All")
            {
                result = result.Where(a =>
                    a.Status == status
                ).ToList();
            }

            return result;
        }
    }
}