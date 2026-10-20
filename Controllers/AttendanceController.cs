using System.Security.Claims;
using BYPManagementSystem.Data;
using BYPManagementSystem.Models;
using BYPManagementSystem.Services;
using BYPManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BYPManagementSystem.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AttendancePhotoStorage _photoStorage;

        public AttendanceController(
            ApplicationDbContext context,
            AttendancePhotoStorage photoStorage)
        {
            _context = context;
            _photoStorage = photoStorage;
        }


        // ==========================================================
        // ATTENDANCE - PROGRAM LIST
        // ADMIN / STAFF
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var programs =
                await _context.YouthPrograms
                    .AsNoTracking()
                    .OrderByDescending(
                        p => p.Status == "Active"
                    )
                    .ThenBy(p => p.Name)
                    .ToListAsync();


            // ------------------------------------------------------
            // PENDING PHOTO REVIEWS
            // Badge for the review queue link.
            // ------------------------------------------------------

            ViewBag.PendingPhotoReviews =
                await _context.AttendancePhotoSubmissions
                    .CountAsync(
                        s => s.Status == "Pending"
                    );


            // --------------------------------------------------
            // SCHEDULE ELIGIBILITY (APPLICATION TIME ZONE)
            // Per program: is it actually scheduled for today?
            // Admin/Staff still see every program (history and
            // management stay reachable), but the list now
            // shows which ones accept attendance today.
            // --------------------------------------------------

            var appToday =
                AttendanceSchedule.NowApp().Date;

            ViewBag.AppToday =
                appToday;

            ViewBag.ScheduledTodayProgramIds =
                programs
                    .Where(p =>
                        string.Equals(
                            p.Status,
                            "Active",
                            StringComparison
                                .OrdinalIgnoreCase) &&
                        AttendanceSchedule.IsScheduledOn(
                            p,
                            appToday))
                    .Select(p => p.Id)
                    .ToHashSet();


            return View(programs);
        }


        // ==========================================================
        // VIEW PARTICIPANTS
        // MAIN TABLE: APPROVED PARTICIPANTS OF THE SELECTED PROGRAM
        // WITH THE PER-ACTION ATTENDANCE STATUS FOR ONE DATE
        // (#, PARTICIPANT NAME, MORNING IN, MORNING OUT, AFTERNOON
        //  IN, AFTERNOON OUT, STATUS, ACTION)
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> Participants(
            int id,
            string? search,
            string? status,
            DateTime? attendanceDate,
            int page = 1)
        {
            var program =
                await _context.YouthPrograms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );

            if (program == null)
            {
                return NotFound();
            }


            var nowApp = AttendanceSchedule.NowApp();

            var date =
                (attendanceDate ?? nowApp.Date).Date;


            // EVERY approved registration is listed, including
            // participants who never submitted a photo.

            var registrations =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .Include(r => r.YouthParticipant)
                    .Where(r =>
                        r.ProgramId == id &&
                        r.Status == "Approved" &&
                        r.YouthParticipantId != null &&
                        r.YouthParticipant != null &&
                        r.YouthParticipant.IsActive)
                    .OrderBy(
                        r => r.YouthParticipant!.LastName
                    )
                    .ThenBy(
                        r => r.YouthParticipant!.FirstName
                    )
                    .ToListAsync();


            var registrationIds =
                registrations
                    .Select(r => r.Id)
                    .ToList();


            // Records and submissions for the selected date only.

            var records =
                registrationIds.Count == 0
                    ? new List<AttendanceRecord>()
                    : await _context.AttendanceRecords
                        .AsNoTracking()
                        .Where(x =>
                            registrationIds.Contains(
                                x.RegistrationId) &&
                            x.AttendanceDate >= date &&
                            x.AttendanceDate <
                                date.AddDays(1))
                        .ToListAsync();

            var submissions =
                registrationIds.Count == 0
                    ? new List<AttendancePhotoSubmission>()
                    : await _context.AttendancePhotoSubmissions
                        .AsNoTracking()
                        .Where(s =>
                            registrationIds.Contains(
                                s.RegistrationId) &&
                            s.AttendanceDate >= date &&
                            s.AttendanceDate <
                                date.AddDays(1))
                        .ToListAsync();

            var recordByRegistration =
                records
                    .GroupBy(x => x.RegistrationId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First());

            var submissionsByRegistration =
                submissions
                    .GroupBy(s => s.RegistrationId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToList());


            var rows =
                new List<ParticipantAttendanceRowViewModel>();

            foreach (var registration in registrations)
            {
                recordByRegistration.TryGetValue(
                    registration.Id,
                    out var dayRecord);

                submissionsByRegistration.TryGetValue(
                    registration.Id,
                    out var daySubs);

                var evaluation =
                    AttendanceStatusCalculator.Evaluate(
                        program,
                        dayRecord,
                        daySubs,
                        date,
                        nowApp);

                rows.Add(
                    new ParticipantAttendanceRowViewModel
                    {
                        RegistrationId = registration.Id,

                        YouthParticipantId =
                            registration.YouthParticipantId ?? 0,

                        FullName =
                            registration.YouthParticipant
                                ?.FullName ??
                            "Unknown Participant",

                        Barangay =
                            registration.YouthParticipant
                                ?.Barangay,

                        RegisteredAt = registration.RegisteredAt,

                        MorningIn =
                            ToCell(evaluation.MorningIn),

                        MorningOut =
                            ToCell(evaluation.MorningOut),

                        AfternoonIn =
                            ToCell(evaluation.AfternoonIn),

                        AfternoonOut =
                            ToCell(evaluation.AfternoonOut),

                        StatusLabel =
                            evaluation.OverallStatus,

                        StatusCss =
                            StatusCssFor(
                                evaluation.OverallStatus)
                    });
            }


            // STATUS FILTER + counts per computed status.

            var statusFilter =
                string.IsNullOrWhiteSpace(status)
                    ? "All"
                    : status.Trim();

            var statusCounts =
                rows
                    .GroupBy(r => r.StatusLabel)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Count());

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                rows =
                    rows
                        .Where(r =>
                            r.FullName.Contains(
                                term,
                                System.StringComparison.OrdinalIgnoreCase) ||
                            (r.Barangay != null &&
                             r.Barangay.Contains(
                                 term,
                                 System.StringComparison.OrdinalIgnoreCase)))
                        .ToList();
            }

            if (statusFilter != "All")
            {
                rows =
                    rows
                        .Where(r =>
                            r.StatusLabel == statusFilter)
                        .ToList();
            }


            // PAGINATION

            const int pageSize = 15;

            var totalCount = rows.Count;

            var totalPages = Math.Max(
                1,
                (int)Math.Ceiling(
                    totalCount / (double)pageSize));

            if (page < 1)
            {
                page = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            var pagedRows =
                rows
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

            for (var i = 0; i < pagedRows.Count; i++)
            {
                pagedRows[i].RowNumber =
                    (page - 1) * pageSize + i + 1;
            }


            // Status filter options in the workflow priority
            // order, each with its live count.

            var statusOptions =
                new List<(string Value, string Label, int Count)>();

            var coreStatuses = new[]
            {
                AttendanceStatusCalculator.PendingReview,
                AttendanceStatusCalculator.Present,
                AttendanceStatusCalculator.Incomplete,
                AttendanceStatusCalculator.Rejected,
                AttendanceStatusCalculator.Absent,
                AttendanceStatusCalculator.NotYetDue
            };

            foreach (var core in coreStatuses)
            {
                statusCounts.TryGetValue(core, out var coreCount);

                statusOptions.Add(
                    (core, core, coreCount));
            }

            // Legacy/manual statuses (Late, Excused, ...) appear
            // only when such rows actually exist.

            foreach (var extra in statusCounts
                .Keys
                .Where(k => !coreStatuses.Contains(k))
                .OrderBy(k => k))
            {
                statusOptions.Add(
                    (extra, extra, statusCounts[extra]));
            }


            ViewBag.Program = program;
            ViewBag.Search = search;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.StatusOptions = statusOptions;
            ViewBag.AttendanceDate = date;
            ViewBag.AppToday = nowApp.Date;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;


            return View(pagedRows);
        }


        private static AttendanceActionCellViewModel ToCell(
            AttendanceStatusCalculator.ActionState state)
        {
            return new AttendanceActionCellViewModel
            {
                Kind = state.Kind,

                TimeDisplay = state.ApprovedTimeApp.HasValue
                    ? state.ApprovedTimeApp.Value
                        .ToString("hh:mm tt")
                    : ""
            };
        }


        private static string StatusCssFor(string status)
        {
            switch (status)
            {
                case AttendanceStatusCalculator.Present:
                case "Complete":
                    return "st-present";

                case AttendanceStatusCalculator.PendingReview:
                    return "st-pending";

                case AttendanceStatusCalculator.Incomplete:
                    return "st-incomplete";

                case AttendanceStatusCalculator.Rejected:
                    return "st-rejected";

                case AttendanceStatusCalculator.Absent:
                    return "st-absent";

                case AttendanceStatusCalculator.NotYetDue:
                    return "st-notyetdue";

                case "Late":
                    return "st-late";

                case "Excused":
                    return "st-excused";

                default:
                    return "st-other";
            }
        }


        // ==========================================================
        // ATTENDANCE RECORDS
        // SELECTED PROGRAM + SELECTED YOUTH
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> Records(
            int programId,
            int youthParticipantId,
            string? search,
            string? status,
            DateTime? attendanceDate)
        {
            // ------------------------------------------------------
            // PROGRAM
            // ------------------------------------------------------

            var program =
                await _context.YouthPrograms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.Id == programId
                    );

            if (program == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // YOUTH
            // ------------------------------------------------------

            var youth =
                await _context.YouthParticipants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        y => y.Id == youthParticipantId
                    );

            if (youth == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // APPROVED REGISTRATION
            // ------------------------------------------------------

            var registration =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r =>
                        r.ProgramId == programId &&
                        r.YouthParticipantId ==
                            youthParticipantId &&
                        r.Status == "Approved"
                    );


            if (registration == null)
            {
                TempData["Error"] =
                    "This youth participant is not an approved participant of this program.";

                return RedirectToAction(
                    nameof(Participants),
                    new
                    {
                        id = programId
                    }
                );
            }


            // ------------------------------------------------------
            // ALL APPROVED REGISTRATIONS
            // Used for program summary.
            // ------------------------------------------------------

            var approvedRegistrations =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .Where(r =>
                        r.ProgramId == programId &&
                        r.Status == "Approved" &&
                        r.YouthParticipantId != null)
                    .ToListAsync();


            var registrationIds =
                approvedRegistrations
                    .Select(r => r.Id)
                    .ToList();


            // ------------------------------------------------------
            // ATTENDANCE QUERY
            // ------------------------------------------------------

            var query =
                _context.AttendanceRecords
                    .AsNoTracking()
                    .Include(a => a.Registration)
                        .ThenInclude(
                            r => r!.YouthParticipant
                        )
                    .Include(a => a.Registration)
                        .ThenInclude(
                            r => r!.Program
                        )
                    .Where(a =>
                        a.Registration != null &&
                        a.Registration.ProgramId ==
                            programId &&
                        a.Registration.YouthParticipantId ==
                            youthParticipantId
                    );


            // ------------------------------------------------------
            // SEARCH
            // ------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(a =>
                    (
                        a.Registration!
                            .YouthParticipant!
                            .FirstName
                        + " " +
                        a.Registration!
                            .YouthParticipant!
                            .LastName
                    ).Contains(search)

                    ||

                    a.Status.Contains(search)

                    ||

                    (
                        a.Notes != null &&
                        a.Notes.Contains(search)
                    )
                );
            }


            // ------------------------------------------------------
            // STATUS FILTER
            // ------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(status) &&
                status != "All")
            {
                query =
                    query.Where(
                        a => a.Status == status
                    );
            }


            // ------------------------------------------------------
            // DATE FILTER
            // ------------------------------------------------------

            if (attendanceDate.HasValue)
            {
                var selectedDate =
                    attendanceDate.Value.Date;

                query =
                    query.Where(a =>
                        a.AttendanceDate.Date ==
                        selectedDate
                    );
            }


            // ------------------------------------------------------
            // GET RECORDS
            // ------------------------------------------------------

            var records =
                await query
                    .OrderByDescending(
                        a => a.AttendanceDate
                    )
                    .ThenByDescending(
                        a => a.RecordedAt
                    )
                    .ToListAsync();


            // ======================================================
            // DAILY PROGRAM SUMMARY
            // ======================================================

            var allProgramAttendance =
                await _context.AttendanceRecords
                    .AsNoTracking()
                    .Where(a =>
                        a.Registration != null &&
                        a.Registration.ProgramId ==
                            programId &&
                        registrationIds.Contains(
                            a.RegistrationId
                        )
                    )
                    .ToListAsync();


            var summaryDate =
                attendanceDate?.Date ??
                DateTime.Today;


            var dailyAttendance =
                allProgramAttendance
                    .Where(a =>
                        a.AttendanceDate.Date ==
                        summaryDate
                    )
                    .ToList();


            var totalStudents =
                approvedRegistrations.Count;


            var presentCount =
                dailyAttendance.Count(
                    a => a.Status == "Present"
                );


            var absentCount =
                dailyAttendance.Count(
                    a => a.Status == "Absent"
                );


            var lateExcusedCount =
                dailyAttendance.Count(
                    a =>
                        a.Status == "Late" ||
                        a.Status == "Excused"
                );


            // ------------------------------------------------------
            // VIEW DATA
            // ------------------------------------------------------

            ViewBag.Program =
                program;

            ViewBag.Youth =
                youth;

            ViewBag.TotalStudents =
                totalStudents;

            ViewBag.PresentCount =
                presentCount;

            ViewBag.AbsentCount =
                absentCount;

            ViewBag.LateExcusedCount =
                lateExcusedCount;

            ViewBag.Search =
                search;

            ViewBag.Status =
                status;

            ViewBag.AttendanceDate =
                attendanceDate?
                    .ToString("yyyy-MM-dd");


            return View(records);
        }


        // ==========================================================
        // RECORD ATTENDANCE - GET
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> Record(
            int programId,
            int youthParticipantId)
        {
            var registration =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .Include(r => r.Program)
                    .Include(r => r.YouthParticipant)
                    .FirstOrDefaultAsync(r =>
                        r.ProgramId == programId &&
                        r.YouthParticipantId ==
                            youthParticipantId &&
                        r.Status == "Approved"
                    );


            if (registration == null)
            {
                return NotFound();
            }


            var model =
                new AttendanceRecord
                {
                    RegistrationId =
                        registration.Id,

                    AttendanceDate =
                        DateTime.Today,

                    Status =
                        "Present"
                };


            ViewBag.Program =
                registration.Program;

            ViewBag.Youth =
                registration.YouthParticipant;


            return View(model);
        }


        // ==========================================================
        // RECORD ATTENDANCE - POST
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Record(
            AttendanceRecord model,
            int programId,
            int youthParticipantId)
        {
            // ------------------------------------------------------
            // FIND AND VERIFY APPROVED REGISTRATION
            // ------------------------------------------------------

            var registration =
                await _context.ProgramRegistrations
                    .Include(r => r.Program)
                    .Include(r => r.YouthParticipant)
                    .FirstOrDefaultAsync(r =>
                        r.Id == model.RegistrationId &&
                        r.ProgramId == programId &&
                        r.YouthParticipantId ==
                            youthParticipantId &&
                        r.Status == "Approved"
                    );


            if (registration == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid approved youth participant."
                );


                ViewBag.Program =
                    null;

                ViewBag.Youth =
                    null;


                return View(model);
            }


            // ------------------------------------------------------
            // PROGRAM STATUS + SCHEDULE (SERVER-SIDE)
            //
            // Manual recording follows the same policy as photo
            // submissions: the program must still be Active, and
            // the chosen attendance date must fall inside the
            // program's structured schedule (application zone).
            // Inactive/completed programs and out-of-schedule
            // dates are rejected before anything is written.
            // ------------------------------------------------------

            var recordProgram =
                registration.Program;


            if (recordProgram != null &&
                !string.Equals(
                    recordProgram.Status,
                    "Active",
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(model.AttendanceDate),
                    "The program is "
                    + recordProgram.Status
                    + ", so no new attendance can be recorded for it."
                );
            }


            if (recordProgram != null &&
                model.AttendanceDate != default &&
                AttendanceSchedule.IsScheduledOn(
                    recordProgram,
                    model.AttendanceDate.Date) ==
                        false)
            {
                ModelState.AddModelError(
                    nameof(model.AttendanceDate),
                    "This program is not scheduled on "
                    + model.AttendanceDate.ToString(
                        "MMM dd, yyyy")
                    + " (" + AttendanceSchedule.ZoneLabel + "), so attendance cannot be recorded for that date."
                );
            }


            // ------------------------------------------------------
            // VALIDATE DATE
            // ------------------------------------------------------

            if (model.AttendanceDate == default)
            {
                ModelState.AddModelError(
                    nameof(model.AttendanceDate),
                    "Attendance date is required."
                );
            }


            // ------------------------------------------------------
            // VALIDATE STATUS
            // ------------------------------------------------------

            var validStatuses =
                new[]
                {
                    "Present",
                    "Absent",
                    "Late",
                    "Excused"
                };


            if (!validStatuses.Contains(
                model.Status))
            {
                ModelState.AddModelError(
                    nameof(model.Status),
                    "Invalid attendance status."
                );
            }


            // ------------------------------------------------------
            // PREVENT DUPLICATE
            // ------------------------------------------------------

            var duplicate =
                await _context.AttendanceRecords
                    .AnyAsync(a =>
                        a.RegistrationId ==
                            registration.Id &&
                        a.AttendanceDate.Date ==
                            model.AttendanceDate.Date
                    );


            if (duplicate)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Attendance has already been recorded for this date."
                );
            }


            // ------------------------------------------------------
            // VALIDATION FAILED
            // ------------------------------------------------------

            if (!ModelState.IsValid)
            {
                ViewBag.Program =
                    registration.Program;

                ViewBag.Youth =
                    registration.YouthParticipant;

                return View(model);
            }


            // ------------------------------------------------------
            // FORCE CORRECT REGISTRATION
            // ------------------------------------------------------

            model.RegistrationId =
                registration.Id;


            model.RecordedAt =
                DateTime.UtcNow;


            _context.AttendanceRecords.Add(
                model
            );


            // ------------------------------------------------------
            // NOTIFICATION - ATTENDANCE RECORDED
            // Added before the same SaveChanges so the
            // notification only persists when the record does.
            // ------------------------------------------------------

            var recordedNotification =
                NotificationHelper.ForRegistration(
                    registration,
                    "Attendance Recorded",
                    $"Your attendance for \"{registration.Program?.Name ?? "the program"}\" on {model.AttendanceDate:MMM dd, yyyy} has been recorded as {model.Status}.",
                    "Attendance");


            if (recordedNotification != null)
            {
                _context.YouthNotifications
                    .Add(recordedNotification);
            }


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // A concurrent request (photo approval or a second
                // manual entry) stored this date's record first.
                // The unique registration + date index rejected
                // ours, so no partial attendance was written.

                ModelState.AddModelError(
                    string.Empty,
                    "Attendance has already been recorded for this date by another request."
                );

                ViewBag.Program =
                    registration.Program;

                ViewBag.Youth =
                    registration.YouthParticipant;

                return View(model);
            }


            TempData["Success"] =
                "Attendance successfully recorded.";


            return RedirectToAction(
                nameof(Records),
                new
                {
                    programId,

                    youthParticipantId
                }
            );
        }


        // ==========================================================
        // EDIT ATTENDANCE - GET
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> Edit(
            int id)
        {
            var record =
                await _context.AttendanceRecords
                    .Include(a => a.Registration)
                        .ThenInclude(
                            r => r!.Program
                        )
                    .Include(a => a.Registration)
                        .ThenInclude(
                            r => r!.YouthParticipant
                        )
                    .FirstOrDefaultAsync(
                        a => a.Id == id
                    );


            if (record == null)
            {
                return NotFound();
            }


            if (record.Registration == null)
            {
                return NotFound();
            }


            if (record.Registration.Status !=
                "Approved")
            {
                TempData["Error"] =
                    "Attendance can only be edited for approved registrations.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            ViewBag.Program =
                record.Registration.Program;

            ViewBag.Youth =
                record.Registration.YouthParticipant;


            return View(record);
        }


        // ==========================================================
        // EDIT ATTENDANCE - POST
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            AttendanceRecord model)
        {
            var record =
                await _context.AttendanceRecords
                    .Include(a => a.Registration)
                        .ThenInclude(
                            r => r!.YouthParticipant)
                    .FirstOrDefaultAsync(
                        a => a.Id == id
                    );


            if (record == null)
            {
                return NotFound();
            }


            if (record.Registration == null)
            {
                return NotFound();
            }


            if (record.Registration.Status !=
                "Approved")
            {
                TempData["Error"] =
                    "Attendance can only be edited for approved registrations.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // ------------------------------------------------------
            // VALID STATUS
            // ------------------------------------------------------

            var validStatuses =
                new[]
                {
                    "Present",
                    "Absent",
                    "Late",
                    "Excused"
                };


            if (!validStatuses.Contains(
                model.Status))
            {
                ModelState.AddModelError(
                    "Status",
                    "Invalid attendance status."
                );
            }


            // ------------------------------------------------------
            // VALID DATE
            // ------------------------------------------------------

            if (model.AttendanceDate == default)
            {
                ModelState.AddModelError(
                    "AttendanceDate",
                    "Attendance date is required."
                );
            }


            // ------------------------------------------------------
            // DUPLICATE DATE CHECK
            // Exclude current attendance record.
            // ------------------------------------------------------

            if (model.AttendanceDate != default)
            {
                var duplicate =
                    await _context.AttendanceRecords
                        .AnyAsync(a =>
                            a.Id != id &&
                            a.RegistrationId ==
                                record.RegistrationId &&
                            a.AttendanceDate.Date ==
                                model.AttendanceDate.Date
                        );


                if (duplicate)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Another attendance record already exists for this date."
                    );
                }
            }


            // ------------------------------------------------------
            // VALIDATION FAILED
            // ------------------------------------------------------

            if (!ModelState.IsValid)
            {
                ViewBag.Program =
                    record.Registration.Program;

                var youth =
                    await _context.YouthParticipants
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            y =>
                                y.Id ==
                                record.Registration!
                                    .YouthParticipantId
                        );

                ViewBag.Youth =
                    youth;

                return View(model);
            }


            // ------------------------------------------------------
            // UPDATE ONLY ALLOWED FIELDS
            // ------------------------------------------------------

            var previousAttendanceStatus =
                record.Status;


            record.AttendanceDate =
                model.AttendanceDate;

            record.Status =
                model.Status;

            record.Notes =
                model.Notes;


            await _context.SaveChangesAsync();


            // ------------------------------------------------------
            // NOTIFICATION - ATTENDANCE UPDATED
            // Only when the attendance status really changed.
            // ------------------------------------------------------

            if (previousAttendanceStatus !=
                model.Status)
            {
                var updatedProgramName =
                    await _context.YouthPrograms
                        .AsNoTracking()
                        .Where(p => p.Id ==
                            record.Registration!
                                .ProgramId)
                        .Select(p => p.Name)
                        .FirstOrDefaultAsync()
                    ?? "the program";


                var updatedNotification =
                    NotificationHelper.ForRegistration(
                        record.Registration!,
                        "Attendance Updated",
                        $"Your attendance for \"{updatedProgramName}\" on {model.AttendanceDate:MMM dd, yyyy} has been updated to {model.Status}.",
                        "Attendance");


                if (updatedNotification != null)
                {
                    _context.YouthNotifications
                        .Add(updatedNotification);


                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception)
                    {
                        // A notification problem must not
                        // fail the attendance update.
                    }
                }
            }


            TempData["Success"] =
                "Attendance successfully updated.";


            // ------------------------------------------------------
            // RETURN TO SAME PROGRAM + YOUTH
            // ------------------------------------------------------

            var registration =
                record.Registration;


            if (registration.YouthParticipantId
                .HasValue)
            {
                return RedirectToAction(
                    nameof(Records),
                    new
                    {
                        programId =
                            registration.ProgramId,

                        youthParticipantId =
                            registration.YouthParticipantId
                                .Value
                    }
                );
            }


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // USER / YOUTH - MY ATTENDANCE
        // ==========================================================

        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> MyAttendance()
        {
            var userId =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;


            if (string.IsNullOrWhiteSpace(
                userId))
            {
                return Unauthorized();
            }


            var userEmail =
                User.Identity?.Name;


            // ------------------------------------------------------
            // BELONGING CHECK VIA PARTICIPANT (FALLBACK)
            // ------------------------------------------------------

            var participantId =
                await _context.YouthParticipants
                    .AsNoTracking()
                    .Where(
                        y => y.UserId == userId
                    )
                    .Select(
                        y => (int?)y.Id
                    )
                    .FirstOrDefaultAsync();


            var records =
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
                    .Where(a =>
                        a.Registration != null &&
                        (
                            a.Registration.UserId ==
                                userId
                            ||
                            (
                                a.Registration.YouthParticipant
                                    != null &&
                                a.Registration.YouthParticipant
                                    .UserId == userId
                            )
                            ||
                            (
                                participantId != null &&
                                a.Registration.YouthParticipantId ==
                                    participantId
                            )
                            ||
                            (
                                !string.IsNullOrWhiteSpace(userEmail) &&
                                a.Registration.UserEmail ==
                                    userEmail
                            )
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
            // APPROVED REGISTRATIONS ELIGIBLE FOR PHOTO SUBMISSION
            // Same ownership rules as the attendance list above.
            // ------------------------------------------------------

            var eligibleRegistrations =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .Include(r => r.Program)
                    .Include(r => r.YouthParticipant)
                    .Where(r =>
                        r.Status == "Approved" &&
                        r.YouthParticipantId != null &&
                        r.YouthParticipant != null &&
                        r.YouthParticipant.IsActive &&
                        (
                            (
                                userId != null &&
                                r.UserId == userId
                            )
                            ||
                            (
                                userId != null &&
                                r.YouthParticipant.UserId ==
                                    userId
                            )
                            ||
                            (
                                !string.IsNullOrWhiteSpace(
                                    userEmail) &&
                                r.UserEmail == userEmail
                            )
                        )
                    )
                    .OrderBy(
                        r => r.Program!.Name
                    )
                    .ToListAsync();


            // ------------------------------------------------------
            // EXISTING PHOTO SUBMISSIONS (REVIEW STATUS)
            // ------------------------------------------------------

            var submissions =
                await _context.AttendancePhotoSubmissions
                    .AsNoTracking()
                    .Include(s => s.Registration)
                        .ThenInclude(
                            r => r!.Program
                        )
                    .Include(s => s.Registration)
                        .ThenInclude(
                            r => r!.YouthParticipant
                        )
                    .Where(s =>
                        s.Registration != null &&
                        (
                            (
                                userId != null &&
                                s.Registration.UserId ==
                                    userId
                            )
                            ||
                            (
                                userId != null &&
                                s.Registration.YouthParticipant
                                    != null &&
                                s.Registration.YouthParticipant
                                    .UserId == userId
                            )
                            ||
                            (
                                !string.IsNullOrWhiteSpace(
                                    userEmail) &&
                                s.Registration.UserEmail ==
                                    userEmail
                            )
                        )
                    )
                    .OrderByDescending(
                        s => s.SubmittedAt
                    )
                    .ToListAsync();


            ViewBag.EligibleRegistrations =
                eligibleRegistrations;

            ViewBag.Submissions =
                submissions;

            ViewBag.MaxPhotoSizeMb =
                Math.Round(
                    _photoStorage.MaxFileSizeBytes
                    / (1024d * 1024d),
                    1);


            // Application-zone "now" so the view can show which
            // attendance windows are currently open.

            ViewBag.NowApp =
                AttendanceSchedule.NowApp();


            return View(records);
        }


        // ==========================================================
        // SUBMIT ATTENDANCE PHOTO
        // USER / YOUTH
        // ==========================================================
        //
        // A photo submission only creates a Pending review item.
        // It never records attendance by itself.
        // ==========================================================

        [Authorize(Roles = "User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> SubmitPhoto(
            int registrationId,
            IFormFile? photo,
            string? action)
        {
            // ------------------------------------------------------
            // CURRENT USER - NEVER TRUSTED FROM THE REQUEST
            // ------------------------------------------------------

            var userId =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;

            var userEmail =
                User.Identity?.Name;


            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }


            // ------------------------------------------------------
            // REGISTRATION
            // ------------------------------------------------------

            var registration =
                await _context.ProgramRegistrations
                    .Include(r => r.Program)
                    .Include(r => r.YouthParticipant)
                    .FirstOrDefaultAsync(r =>
                        r.Id == registrationId
                    );


            if (registration == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // OWNERSHIP + ELIGIBILITY
            // Only the youth's own approved registration with an
            // active participant record can receive a photo.
            // ------------------------------------------------------

            if (!OwnsRegistration(
                    registration,
                    userId,
                    userEmail) ||
                registration.Status != "Approved" ||
                registration.YouthParticipant == null ||
                !registration.YouthParticipant.IsActive)
            {
                TempData["Error"] =
                    "You can only submit an attendance photo for your own approved program registration.";

                return RedirectToAction(
                    nameof(MyAttendance)
                );
            }


            // ------------------------------------------------------
            // FILE VALIDATION
            // Extension, content type, size, and magic bytes.
            // ------------------------------------------------------

            var validationError =
                await _photoStorage.ValidateAsync(
                    photo);


            if (validationError != null)
            {
                TempData["Error"] =
                    validationError;

                return RedirectToAction(
                    nameof(MyAttendance)
                );
            }


            // ------------------------------------------------------
            // ATTENDANCE ACTION + WINDOW (PHILIPPINE TIME)
            // The submission time is the server clock - never a
            // browser-supplied timestamp. A submission is only
            // accepted inside the configured window of the chosen
            // action (inclusive bounds).
            // ------------------------------------------------------

            var attendanceAction =
                AttendanceSchedule.NormalizeAction(
                    action);


            var program =
                registration.Program;


            if (program == null)
            {
                TempData["Error"] =
                    "The program schedule could not be loaded. Please try again.";

                return RedirectToAction(
                    nameof(MyAttendance)
                );
            }


            var manilaNow =
                AttendanceSchedule.NowApp();

            var attendanceDate =
                manilaNow.Date;

            var nowTime =
                TimeOnly.FromDateTime(manilaNow);


            // ------------------------------------------------------
            // PROGRAM ELIGIBILITY (SERVER-SIDE, PHILIPPINE DATE)
            // The program must exist (checked above), have a
            // usable schedule, be Active, and actually be
            // scheduled for today's Philippine date - so
            // future-dated, already-ended, or inactive
            // programs never accept attendance. The client
            // cannot influence this check.
            // ------------------------------------------------------

            if (AttendanceSchedule.HasInvalidSchedule(
                    program))
            {
                TempData["Error"] =
                    "This program has an invalid schedule, so attendance cannot be submitted. Please contact the program administrator.";

                return RedirectToAction(
                    nameof(MyAttendance)
                );
            }


            if (!string.Equals(
                    program.Status,
                    "Active",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "This program is not active, so attendance cannot be submitted.";

                return RedirectToAction(
                    nameof(MyAttendance)
                );
            }


            if (!AttendanceSchedule.IsScheduledOn(
                    program,
                    attendanceDate))
            {
                TempData["Error"] =
                    "This program is not scheduled for "
                    + attendanceDate.ToString("MMM dd, yyyy")
                    + " (" + AttendanceSchedule.ZoneLabel + "), so attendance cannot be submitted today.";

                return RedirectToAction(
                    nameof(MyAttendance)
                );
            }


            var (windowStart, windowEnd) =
                AttendanceSchedule.WindowFor(
                    program,
                    attendanceAction);


            if (!AttendanceSchedule.IsWithin(
                    nowTime,
                    windowStart,
                    windowEnd))
            {
                TempData["Error"] =
                    AttendanceSchedule.ActionLabel(attendanceAction)
                    + " can only be submitted between "
                    + AttendanceSchedule.FormatTime(windowStart)
                    + " and "
                    + AttendanceSchedule.FormatTime(windowEnd)
                    + " (" + AttendanceSchedule.ZoneLabel + "). It is currently "
                    + AttendanceSchedule.FormatTime(nowTime)
                    + ".";

                return RedirectToAction(
                    nameof(MyAttendance)
                );
            }


            // ------------------------------------------------------
            // VALID SEQUENCE (FOUR ACTIONS)
            // A Time Out requires its own session's Time In to be
            // approved first: Morning Out needs Morning In, and
            // Afternoon Out needs Afternoon In. A Time In is never
            // submitted twice, whether morning or afternoon.
            // ------------------------------------------------------

            var sessionInAction =
                AttendanceSchedule.SessionInFor(
                    attendanceAction);

            var existingRecord =
                await _context.AttendanceRecords
                    .FirstOrDefaultAsync(a =>
                        a.RegistrationId ==
                            registration.Id &&
                        a.AttendanceDate ==
                            attendanceDate
                    );


            var sessionInRecorded =
                existingRecord != null &&
                AttendanceSchedule.RecordedTimeFor(
                    existingRecord,
                    sessionInAction).HasValue;


            var isMorningSession =
                AttendanceSchedule.IsMorningAction(
                    sessionInAction);


            // Canonical rows store the new action values; legacy
            // rows store the old single-session values ("TimeIn"),
            // which map to the morning session.

            var approvedSessionIn =
                sessionInRecorded ||
                await _context.AttendancePhotoSubmissions
                    .AnyAsync(s =>
                        s.RegistrationId ==
                            registration.Id &&
                        s.AttendanceDate ==
                            attendanceDate &&
                        s.Status ==
                            "Approved" &&
                        (isMorningSession
                            ? (s.Action ==
                                sessionInAction ||
                                s.Action ==
                                    "TimeIn")
                            : s.Action ==
                                sessionInAction)
                    );


            if (AttendanceSchedule.IsTimeOutAction(
                    attendanceAction))
            {
                if (!approvedSessionIn)
                {
                    TempData["Error"] =
                        "A "
                        + AttendanceSchedule.ActionLabel(attendanceAction)
                        + " can only be submitted after your "
                        + AttendanceSchedule.ActionLabel(sessionInAction)
                        + " has been approved for the same day.";

                    return RedirectToAction(
                        nameof(MyAttendance)
                    );
                }


                var outAlreadyRecorded =
                    existingRecord != null &&
                    AttendanceSchedule.RecordedTimeFor(
                        existingRecord,
                        attendanceAction).HasValue;

                if (outAlreadyRecorded)
                {
                    TempData["Error"] =
                        "Your "
                        + AttendanceSchedule.ActionLabel(attendanceAction)
                        + " has already been recorded for today.";

                    return RedirectToAction(
                        nameof(MyAttendance)
                    );
                }
            }
            else
            {
                var inAlreadyRecorded =
                    existingRecord != null &&
                    AttendanceSchedule.RecordedTimeFor(
                        existingRecord,
                        attendanceAction).HasValue;

                if (inAlreadyRecorded)
                {
                    TempData["Error"] =
                        "Your "
                        + AttendanceSchedule.ActionLabel(attendanceAction)
                        + " has already been recorded for today.";

                    return RedirectToAction(
                        nameof(MyAttendance)
                    );
                }
            }


            // ------------------------------------------------------
            // ONE SUBMISSION PER REGISTRATION PER DATE PER ACTION
            // The duplicate check and the insert run inside one
            // transaction that first locks the registration row
            // (SELECT ... FOR UPDATE). Concurrent submissions for
            // the same registration wait on that lock and then
            // re-read the row, so a second racing request can
            // never pass the check after the first committed.
            // Rejected submissions may be replaced.
            // ------------------------------------------------------

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();


            await _context.Database.ExecuteSqlRawAsync(
                "SELECT Id FROM ProgramRegistrations WHERE Id = {0} FOR UPDATE",
                registration.Id);


            var alreadySubmitted =
                await _context.AttendancePhotoSubmissions
                    .AnyAsync(s =>
                        s.RegistrationId ==
                            registration.Id &&
                        s.AttendanceDate ==
                            attendanceDate &&
                        s.Action ==
                            attendanceAction &&
                        s.Status != "Rejected"
                    );


            if (alreadySubmitted)
            {
                TempData["Error"] =
                    "You have already submitted a "
                    + AttendanceSchedule.ActionLabel(attendanceAction)
                    + " photo for this program today.";

                // Disposal rolls the empty transaction back.
                return RedirectToAction(
                    nameof(MyAttendance)
                );
            }


            // ------------------------------------------------------
            // SAVE FILE + SUBMISSION
            // If the database save fails the stored file is
            // removed again so no orphan files are left behind,
            // and the transaction rolls back (no partial row).
            // ------------------------------------------------------

            string? savedFileName =
                null;


            try
            {
                var saved =
                    await _photoStorage.SaveAsync(
                        photo!);

                savedFileName =
                    saved.FileName;


                var submission =
                    new AttendancePhotoSubmission
                    {
                        RegistrationId =
                            registration.Id,

                        AttendanceDate =
                            attendanceDate,

                        Action =
                            attendanceAction,

                        FileName =
                            saved.FileName,

                        // Detected from the file content,
                        // not from the browser.

                        ContentType =
                            saved.ContentType,

                        FileSizeBytes =
                            photo!.Length,

                        SubmittedByUserId =
                            userId,

                        SubmittedAt =
                            DateTime.UtcNow,

                        Status =
                            "Pending"
                    };


                _context.AttendancePhotoSubmissions
                    .Add(submission);


                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                if (savedFileName != null)
                {
                    _photoStorage.Delete(
                        savedFileName);
                }


                TempData["Error"] =
                    "The attendance photo could not be submitted. Please try again.";

                return RedirectToAction(
                    nameof(MyAttendance)
                );
            }


            TempData["Success"] =
                AttendanceSchedule.ActionLabel(attendanceAction)
                + " photo submitted. It is now waiting for staff review.";

            return RedirectToAction(
                nameof(MyAttendance)
            );
        }


        // ==========================================================
        // VIEW SUBMITTED PHOTO
        // OWNER (YOUTH) OR ADMIN / STAFF
        // ==========================================================
        //
        // Serves the stored file directly. The caller must be
        // either Admin/Staff or the youth who owns the
        // registration - the browser never supplies a path.
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> SubmissionPhoto(
            int id)
        {
            var submission =
                await _context.AttendancePhotoSubmissions
                    .AsNoTracking()
                    .Include(s => s.Registration)
                        .ThenInclude(
                            r => r!.YouthParticipant
                        )
                    .FirstOrDefaultAsync(
                        s => s.Id == id
                    );


            if (submission == null)
            {
                return NotFound();
            }


            var userId =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;

            var userEmail =
                User.Identity?.Name;


            var isStaff =
                User.IsInRole("Admin") ||
                User.IsInRole("Staff");


            if (!isStaff)
            {
                if (submission.Registration == null ||
                    !OwnsRegistration(
                        submission.Registration,
                        userId,
                        userEmail))
                {
                    return NotFound();
                }
            }


            var stream =
                _photoStorage.OpenRead(
                    submission.FileName);


            if (stream == null)
            {
                return NotFound();
            }


            // Private content: keep it out of shared caches.

            Response.Headers.CacheControl =
                "private, no-store";


            return File(
                stream,
                AttendancePhotoStorage.ResolveContentType(
                    submission.ContentType));
        }


        // ==========================================================
        // PHOTO REVIEW QUEUE
        // ADMIN / STAFF
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> PhotoReviews(
            string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                status = "Pending";
            }


            var query =
                _context.AttendancePhotoSubmissions
                    .AsNoTracking()
                    .Include(s => s.Registration)
                        .ThenInclude(
                            r => r!.Program
                        )
                    .Include(s => s.Registration)
                        .ThenInclude(
                            r => r!.YouthParticipant
                        )
                    .AsQueryable();


            if (status != "All")
            {
                query =
                    query.Where(
                        s => s.Status == status
                    );
            }


            var submissions =
                await query
                    .OrderByDescending(
                        s => s.SubmittedAt
                    )
                    .ToListAsync();


            ViewBag.Status =
                status;

            ViewBag.PendingCount =
                await _context.AttendancePhotoSubmissions
                    .CountAsync(
                        s => s.Status == "Pending"
                    );

            ViewBag.ApprovedCount =
                await _context.AttendancePhotoSubmissions
                    .CountAsync(
                        s => s.Status == "Approved"
                    );

            ViewBag.RejectedCount =
                await _context.AttendancePhotoSubmissions
                    .CountAsync(
                        s => s.Status == "Rejected"
                    );


            return View(submissions);
        }


        // ==========================================================
        // PHOTO REVIEW DETAIL
        // ADMIN / STAFF
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> PhotoReview(
            int id)
        {
            var submission =
                await _context.AttendancePhotoSubmissions
                    .AsNoTracking()
                    .Include(s => s.Registration)
                        .ThenInclude(
                            r => r!.Program
                        )
                    .Include(s => s.Registration)
                        .ThenInclude(
                            r => r!.YouthParticipant
                        )
                    .FirstOrDefaultAsync(
                        s => s.Id == id
                    );


            if (submission == null)
            {
                return NotFound();
            }


            return View(submission);
        }


        // ==========================================================
        // APPROVE PHOTO
        // ADMIN / STAFF
        // ==========================================================
        //
        // Re-validates the pending submission against the
        // configured window using the ORIGINAL server-recorded
        // submission time, then records the attendance action
        // with that submission time - never the later approval
        // time.
        //
        // Time In  -> opens the day's attendance record
        //             (Status "InProgress", TimeInAt set).
        // Time Out -> completes it (Status "Present",
        //             TimeOutAt set) and requires an approved
        //             Time In for the same date.
        //
        // Existing attendance is never duplicated - the unique
        // index on registration + date is respected by updating
        // the existing row instead.
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePhoto(
            int id)
        {
            // ------------------------------------------------------
            // ONE REVIEW AT A TIME
            // The submission row is locked for this transaction
            // (SELECT ... FOR UPDATE) before anything is read, so
            // a repeated or concurrent approval request waits
            // here, re-reads the row afterwards, and then sees
            // "already reviewed" instead of recording attendance
            // twice. Disposal rolls the transaction back whenever
            // this action returns without committing, so either
            // the whole approval (review metadata + attendance +
            // notification) is saved together or not at all.
            // ------------------------------------------------------

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();


            await _context.Database.ExecuteSqlRawAsync(
                "SELECT Id FROM AttendancePhotoSubmissions WHERE Id = {0} FOR UPDATE",
                id);


            var submission =
                await _context.AttendancePhotoSubmissions
                    .Include(s => s.Registration)
                        .ThenInclude(
                            r => r!.Program
                        )
                    .FirstOrDefaultAsync(
                        s => s.Id == id
                    );


            if (submission == null)
            {
                return NotFound();
            }


            if (submission.Status != "Pending")
            {
                TempData["Error"] =
                    "This photo submission has already been reviewed.";

                return RedirectToAction(
                    nameof(PhotoReviews),
                    new
                    {
                        status = "Pending"
                    }
                );
            }


            if (submission.Registration == null ||
                submission.Registration.Status !=
                    "Approved")
            {
                TempData["Error"] =
                    "Attendance can only be recorded for approved registrations.";

                return RedirectToAction(
                    nameof(PhotoReviews),
                    new
                    {
                        status = "Pending"
                    }
                );
            }


            var reviewerId =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;

            var reviewerName =
                User.Identity?.Name ?? "Staff";


            var attendanceDate =
                submission.AttendanceDate.Date;


            // ------------------------------------------------------
            // ACTION + ORIGINAL SUBMISSION TIME (PHILIPPINE TIME)
            // ------------------------------------------------------

            var program =
                submission.Registration.Program;


            if (program == null)
            {
                TempData["Error"] =
                    "The program schedule could not be loaded. Attendance was not recorded.";

                return RedirectToAction(
                    nameof(PhotoReviews),
                    new
                    {
                        status = "Pending"
                    }
                );
            }


            // ------------------------------------------------------
            // PROGRAM RE-VALIDATION AT APPROVAL TIME
            // The program must still be Active and the ORIGINAL
            // submission date must still fall inside the
            // program's structured schedule range. A submission
            // for a program that was deactivated or rescheduled
            // out from under it is not recorded; the pending
            // photo itself is preserved for review.
            // ------------------------------------------------------

            if (AttendanceSchedule.HasInvalidSchedule(
                    program))
            {
                TempData["Error"] =
                    "The program schedule for this submission is invalid, so attendance was not recorded. The photo remains pending.";

                return RedirectToAction(
                    nameof(PhotoReviews),
                    new
                    {
                        status = "Pending"
                    }
                );
            }


            if (!string.Equals(
                    program.Status,
                    "Active",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "The program is no longer active, so attendance was not recorded. The photo remains pending.";

                return RedirectToAction(
                    nameof(PhotoReviews),
                    new
                    {
                        status = "Pending"
                    }
                );
            }


            if (!AttendanceSchedule.IsScheduledOn(
                    program,
                    attendanceDate))
            {
                TempData["Error"] =
                    "This program is not scheduled for "
                    + attendanceDate.ToString("MMM dd, yyyy")
                    + " (" + AttendanceSchedule.ZoneLabel + "), so attendance was not recorded. The photo remains pending.";

                return RedirectToAction(
                    nameof(PhotoReviews),
                    new
                    {
                        status = "Pending"
                    }
                );
            }


            var attendanceAction =
                AttendanceSchedule.NormalizeAction(
                    submission.Action);

            var isTimeOut =
                AttendanceSchedule.IsTimeOutAction(
                    attendanceAction);


            var submittedManila =
                AttendanceSchedule.ToAppTime(
                    submission.SubmittedAt);

            var submittedTime =
                TimeOnly.FromDateTime(submittedManila);


            var (windowStart, windowEnd) =
                AttendanceSchedule.WindowFor(
                    program,
                    attendanceAction);


            if (!AttendanceSchedule.IsWithin(
                    submittedTime,
                    windowStart,
                    windowEnd))
            {
                TempData["Error"] =
                    "This "
                    + AttendanceSchedule.ActionLabel(attendanceAction)
                    + " was submitted at "
                    + AttendanceSchedule.FormatTime(submittedTime)
                    + ", outside the allowed window ("
                    + AttendanceSchedule.FormatTime(windowStart)
                    + " \u2013 "
                    + AttendanceSchedule.FormatTime(windowEnd)
                    + ", " + AttendanceSchedule.ZoneLabel + "). Attendance was not recorded.";

                return RedirectToAction(
                    nameof(PhotoReviews),
                    new
                    {
                        status = "Pending"
                    }
                );
            }


            var recordedTimeText =
                AttendanceSchedule.FormatTime(
                    submittedTime);


            var attendanceNote =
                AttendanceSchedule.ActionLabel(attendanceAction)
                + " photo approved by "
                + reviewerName
                + " on "
                + DateTime.UtcNow.ToString("MMM dd, yyyy")
                + "; original submission time "
                + recordedTimeText
                + " (" + AttendanceSchedule.ZoneLabel + ") recorded.";


            try
            {
                // --------------------------------------------------
                // CREATE OR UPDATE - NEVER DUPLICATE
                // Any action writes its own session column via the
                // shared helper, so submission and approval can
                // never disagree about where a time belongs.
                // --------------------------------------------------

                var existingRecord =
                    await _context.AttendanceRecords
                        .FirstOrDefaultAsync(a =>
                            a.RegistrationId ==
                                submission.RegistrationId &&
                            a.AttendanceDate.Date ==
                                attendanceDate
                        );


                if (AttendanceSchedule.RecordedTimeFor(
                        existingRecord ?? new AttendanceRecord(),
                        attendanceAction).HasValue)
                {
                    TempData["Error"] =
                        "A "
                        + AttendanceSchedule.ActionLabel(attendanceAction)
                        + " has already been recorded for this date.";

                    return RedirectToAction(
                        nameof(PhotoReviews),
                        new
                        {
                            status = "Pending"
                        }
                    );
                }


                if (isTimeOut)
                {
                    // ----------------------------------------------
                    // TIME OUT - REQUIRES ITS SESSION'S TIME IN
                    // Morning Out needs Morning In; Afternoon Out
                    // needs Afternoon In.
                    // ----------------------------------------------

                    var sessionInAction =
                        AttendanceSchedule.SessionInFor(
                            attendanceAction);

                    var sessionInRecorded =
                        existingRecord != null &&
                        AttendanceSchedule.RecordedTimeFor(
                            existingRecord,
                            sessionInAction).HasValue;

                    var isMorningSession =
                        AttendanceSchedule.IsMorningAction(
                            sessionInAction);

                    var sessionInApproved =
                        sessionInRecorded ||
                        await _context.AttendancePhotoSubmissions
                            .AnyAsync(s =>
                                s.RegistrationId ==
                                    submission.RegistrationId &&
                                s.AttendanceDate ==
                                    attendanceDate &&
                                s.Status ==
                                    "Approved" &&
                                (isMorningSession
                                    ? (s.Action ==
                                        sessionInAction ||
                                        s.Action ==
                                            "TimeIn")
                                    : s.Action ==
                                        sessionInAction)
                            );

                    if (!sessionInApproved)
                    {
                        TempData["Error"] =
                            "A "
                            + AttendanceSchedule.ActionLabel(attendanceAction)
                            + " can only be recorded after the approved "
                            + AttendanceSchedule.ActionLabel(sessionInAction)
                            + " for the same date.";

                        return RedirectToAction(
                            nameof(PhotoReviews),
                            new
                            {
                                status = "Pending"
                            }
                        );
                    }
                }


                if (existingRecord == null)
                {
                    existingRecord =
                        new AttendanceRecord
                        {
                            RegistrationId =
                                submission
                                    .RegistrationId,

                            AttendanceDate =
                                attendanceDate,

                            Notes =
                                attendanceNote,

                            RecordedAt =
                                DateTime.UtcNow
                        };

                    _context.AttendanceRecords.Add(
                        existingRecord);
                }
                else
                {
                    existingRecord.Notes =
                        MergeAttendanceNote(
                            existingRecord.Notes,
                            attendanceNote);
                }


                AttendanceSchedule.SetRecordedTime(
                    existingRecord,
                    attendanceAction,
                    submission.SubmittedAt);


                existingRecord.Status =
                    AttendanceSchedule.DayStatus(
                        existingRecord);


                // --------------------------------------------------
                // REVIEW METADATA
                // --------------------------------------------------

                submission.Status =
                    "Approved";

                submission.ReviewedAt =
                    DateTime.UtcNow;

                submission.ReviewedByUserId =
                    reviewerId;

                submission.ReviewedByUserName =
                    reviewerName;


                // --------------------------------------------------
                // NOTIFICATION - APPROVED
                // Added before the same SaveChanges so the
                // notification only persists when the review does.
                // --------------------------------------------------

                var programName =
                    program.Name;


                var actionLabelText =
                    AttendanceSchedule.ActionLabel(
                        attendanceAction);


                var approvedNotification =
                    NotificationHelper.ForRegistration(
                        submission.Registration,
                        "Attendance " + actionLabelText + " Approved",
                        $"Your {actionLabelText} photo for \"{programName}\" on {attendanceDate:MMM dd, yyyy} has been approved. Your {actionLabelText} was recorded at {recordedTimeText} ({AttendanceSchedule.ZoneLabel}) with the original submission time.{(attendanceAction == AttendanceSchedule.AfternoonOutAction ? " Your attendance for this day is now complete." : string.Empty)}",
                        "Attendance");


                if (approvedNotification != null)
                {
                    _context.YouthNotifications
                        .Add(approvedNotification);
                }


                // Review metadata, attendance record changes,
                // and the notification commit together.

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                // Includes the unique registration + date index,
                // so a concurrent manual entry cannot create a
                // duplicate attendance record.

                TempData["Error"] =
                    "The photo could not be approved because an attendance record already exists for this date. Please refresh and try again.";

                return RedirectToAction(
                    nameof(PhotoReviews),
                    new
                    {
                        status = "Pending"
                    }
                );
            }


            TempData["Success"] =
                "Photo approved. "
                + AttendanceSchedule.ActionLabel(attendanceAction)
                + " recorded at the original submission time "
                + recordedTimeText
                + " (" + AttendanceSchedule.ZoneLabel + ").";

            return RedirectToAction(
                nameof(PhotoReviews),
                new
                {
                    status = "Pending"
                }
            );
        }


        // ==========================================================
        // REJECT PHOTO
        // ADMIN / STAFF
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPhoto(
            int id,
            string? rejectionReason)
        {
            // Serialized against a concurrent approval or second
            // rejection of the same submission: the row lock is
            // taken before the status is read, so exactly one
            // review wins and the other request sees
            // "already reviewed".

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();


            await _context.Database.ExecuteSqlRawAsync(
                "SELECT Id FROM AttendancePhotoSubmissions WHERE Id = {0} FOR UPDATE",
                id);


            var submission =
                await _context.AttendancePhotoSubmissions
                    .Include(s => s.Registration)
                        .ThenInclude(
                            r => r!.Program
                        )
                    .Include(s => s.Registration)
                        .ThenInclude(
                            r => r!.YouthParticipant
                        )
                    .FirstOrDefaultAsync(
                        s => s.Id == id
                    );


            if (submission == null)
            {
                return NotFound();
            }


            if (submission.Status != "Pending")
            {
                TempData["Error"] =
                    "This photo submission has already been reviewed.";

                return RedirectToAction(
                    nameof(PhotoReviews),
                    new
                    {
                        status = "Pending"
                    }
                );
            }


            // ------------------------------------------------------
            // REJECTION REASON IS REQUIRED
            // ------------------------------------------------------

            rejectionReason =
                rejectionReason?.Trim();


            if (string.IsNullOrWhiteSpace(
                rejectionReason))
            {
                ModelState.AddModelError(
                    "RejectionReason",
                    "A rejection reason is required."
                );
            }
            else if (rejectionReason.Length > 500)
            {
                ModelState.AddModelError(
                    "RejectionReason",
                    "The rejection reason cannot exceed 500 characters."
                );
            }


            if (!ModelState.IsValid)
            {
                return View(
                    nameof(PhotoReview),
                    submission);
            }


            var reviewerId =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;

            var reviewerName =
                User.Identity?.Name ?? "Staff";


            submission.Status =
                "Rejected";

            submission.RejectionReason =
                rejectionReason;

            submission.ReviewedAt =
                DateTime.UtcNow;

            submission.ReviewedByUserId =
                reviewerId;

            submission.ReviewedByUserName =
                reviewerName;


            // ------------------------------------------------------
            // NOTIFICATION - REJECTED
            // ------------------------------------------------------

            if (submission.Registration != null)
            {
                var rejectedNotification =
                    NotificationHelper.ForRegistration(
                        submission.Registration,
                        "Attendance Photo Rejected",
                        $"Your attendance photo for \"{submission.Registration.Program?.Name ?? "the program"}\" on {submission.AttendanceDate:MMM dd, yyyy} was not accepted. Reason: {rejectionReason}",
                        "Attendance");


                if (rejectedNotification != null)
                {
                    _context.YouthNotifications
                        .Add(rejectedNotification);
                }
            }


            try
            {
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "The rejection could not be saved. Please try again.";

                return RedirectToAction(
                    nameof(PhotoReviews),
                    new
                    {
                        status = "Pending"
                    }
                );
            }


            TempData["Success"] =
                "Photo rejected. The youth has been notified.";

            return RedirectToAction(
                nameof(PhotoReviews),
                new
                {
                    status = "Pending"
                }
            );
        }


        // ==========================================================
        // OWNERSHIP CHECK
        // Mirrors the registration matching used by the youth
        // attendance list: user id, linked participant, or the
        // verified account email - never request-supplied ids.
        // ==========================================================

        private static bool OwnsRegistration(
            ProgramRegistration registration,
            string? userId,
            string? userEmail)
        {
            if (!string.IsNullOrWhiteSpace(userId) &&
                registration.UserId == userId)
            {
                return true;
            }


            if (!string.IsNullOrWhiteSpace(userId) &&
                registration.YouthParticipant?.UserId ==
                    userId)
            {
                return true;
            }


            if (!string.IsNullOrWhiteSpace(userEmail) &&
                registration.UserEmail == userEmail)
            {
                return true;
            }


            return false;
        }


        // ==========================================================
        // MERGE ATTENDANCE NOTES
        // Keeps the note within the 500 character limit.
        // ==========================================================

        private static string? MergeAttendanceNote(
            string? existing,
            string note)
        {
            if (string.IsNullOrWhiteSpace(existing))
            {
                return note.Length <= 500
                    ? note
                    : note[..500];
            }


            if (existing.Contains(
                note,
                StringComparison.Ordinal))
            {
                return existing;
            }


            var merged =
                existing + " " + note;

            return merged.Length <= 500
                ? merged
                : merged[..500];
        }
    }
}
