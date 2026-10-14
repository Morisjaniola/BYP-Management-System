using System.Security.Claims;
using BYPManagementSystem.Data;
using BYPManagementSystem.Models;
using BYPManagementSystem.Services;
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


            return View(programs);
        }


        // ==========================================================
        // SELECT PROGRAM
        // SHOW APPROVED YOUTH PARTICIPANTS
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> Participants(
            int id)
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


            var participants =
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


            ViewBag.Program = program;


            return View(participants);
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


            await _context.SaveChangesAsync();


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
            IFormFile? photo)
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
            // ONE SUBMISSION PER REGISTRATION PER DATE
            // Rejected submissions may be replaced.
            // ------------------------------------------------------

            var attendanceDate =
                DateTime.Today;


            var alreadySubmitted =
                await _context.AttendancePhotoSubmissions
                    .AnyAsync(s =>
                        s.RegistrationId ==
                            registration.Id &&
                        s.AttendanceDate ==
                            attendanceDate &&
                        s.Status != "Rejected"
                    );


            if (alreadySubmitted)
            {
                TempData["Error"] =
                    "You have already submitted an attendance photo for this program today.";

                return RedirectToAction(
                    nameof(MyAttendance)
                );
            }


            // ------------------------------------------------------
            // SAVE FILE + SUBMISSION
            // If the database save fails the stored file is
            // removed again so no orphan files are left behind.
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
                "Attendance photo submitted. It is now waiting for staff review.";

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
        // Creates or updates the Present attendance record for
        // the submission date. Existing attendance is never
        // duplicated - the unique index on registration + date is
        // respected by updating the existing row instead.
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePhoto(
            int id)
        {
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


            var attendanceNote =
                "Attendance photo approved by "
                + reviewerName
                + " on "
                + DateTime.UtcNow.ToString("MMM dd, yyyy")
                + ".";


            try
            {
                // --------------------------------------------------
                // CREATE OR UPDATE - NEVER DUPLICATE
                // --------------------------------------------------

                var existingRecord =
                    await _context.AttendanceRecords
                        .FirstOrDefaultAsync(a =>
                            a.RegistrationId ==
                                submission.RegistrationId &&
                            a.AttendanceDate.Date ==
                                attendanceDate
                        );


                if (existingRecord != null)
                {
                    existingRecord.Status =
                        "Present";

                    existingRecord.Notes =
                        MergeAttendanceNote(
                            existingRecord.Notes,
                            attendanceNote);
                }
                else
                {
                    _context.AttendanceRecords.Add(
                        new AttendanceRecord
                        {
                            RegistrationId =
                                submission
                                    .RegistrationId,

                            AttendanceDate =
                                attendanceDate,

                            Status =
                                "Present",

                            Notes =
                                attendanceNote,

                            RecordedAt =
                                DateTime.UtcNow
                        });
                }


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

                var approvedNotification =
                    NotificationHelper.ForRegistration(
                        submission.Registration,
                        "Attendance Photo Approved",
                        $"Your attendance photo for \"{submission.Registration.Program?.Name ?? "the program"}\" on {attendanceDate:MMM dd, yyyy} has been approved. Your attendance is recorded as Present.",
                        "Attendance");


                if (approvedNotification != null)
                {
                    _context.YouthNotifications
                        .Add(approvedNotification);
                }


                await _context.SaveChangesAsync();
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
                "Photo approved and attendance recorded as Present.";

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
