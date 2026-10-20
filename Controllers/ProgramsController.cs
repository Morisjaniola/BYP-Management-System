using BYPManagementSystem.Data;
using BYPManagementSystem.Models;
using BYPManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BYPManagementSystem.Controllers
{
    [Authorize]
    public class ProgramsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AttendancePhotoStorage _photoStorage;
        private readonly ILogger<ProgramsController> _logger;

        public ProgramsController(
            ApplicationDbContext context,
            AttendancePhotoStorage photoStorage,
            ILogger<ProgramsController> logger)
        {
            _context = context;
            _photoStorage = photoStorage;
            _logger = logger;
        }


        // ==========================================================
        // PROGRAM LIST
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            IQueryable<YouthProgram> query = _context.YouthPrograms.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(searchTerm) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                    (p.Schedule != null && p.Schedule.ToLower().Contains(searchTerm)) ||
                    (p.Barangay != null && p.Barangay.ToLower().Contains(searchTerm)) ||
                    p.Status.ToLower().Contains(searchTerm));
            }

            ViewBag.Search = search;

            var programs = await query.ToListAsync();

            // ------------------------------------------------------
            // USER / YOUTH
            // Only active programs are visible.
            // ------------------------------------------------------

            if (User.IsInRole("User"))
            {
                programs = programs
                    .Where(p => p.Status == "Active")
                    .OrderByDescending(p => p.CreatedAt)
                    .ThenBy(p => p.Name)
                    .ToList();
            }
            else
            {
                // --------------------------------------------------
                // ADMIN / STAFF
                // Can see all programs.
                // --------------------------------------------------

                programs = programs
                    .OrderByDescending(p => p.CreatedAt)
                    .ThenBy(p => p.Name)
                    .ToList();
            }

            return View(programs);
        }


        // ==========================================================
        // PROGRAM DETAILS
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var program = await _context.YouthPrograms
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.Id == id
                );

            if (program == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // YOUTH ACCESS RULE (PHASE 5)
            //
            // An authenticated Youth may only open ACTIVE programs.
            // Typing a direct URL to an inactive or completed program
            // redirects them back to the program list. Admin and Staff
            // keep full access for management and history review.
            // ------------------------------------------------------

            if (User.IsInRole("User") &&
                program.Status != "Active")
            {
                return RedirectToAction(
                    nameof(Index)
                );
            }


            // ------------------------------------------------------
            // GET APPROVED PARTICIPANTS
            //
            // IMPORTANT:
            // Use YouthParticipantId.
            //
            // Do NOT use UserId here because a youth participant
            // can exist even without a linked Identity account.
            // ------------------------------------------------------

            var participants =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .Include(r => r.YouthParticipant)
                    .Where(
                        r =>
                            r.ProgramId == id &&
                            r.Status == "Approved" &&
                            r.YouthParticipantId != null &&
                            r.YouthParticipant != null &&
                            r.YouthParticipant.IsActive
                    )
                    .OrderBy(
                        r =>
                            r.YouthParticipant!.LastName
                    )
                    .ThenBy(
                        r =>
                            r.YouthParticipant!.FirstName
                    )
                    .Select(
                        r =>
                            r.YouthParticipant!
                    )
                    .ToListAsync();


            // ------------------------------------------------------
            // COUNTS
            // ------------------------------------------------------

            var approvedCount =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .CountAsync(
                        r =>
                            r.ProgramId == id &&
                            r.Status == "Approved" &&
                            r.YouthParticipantId != null
                    );


            var pendingCount =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .CountAsync(
                        r =>
                            r.ProgramId == id &&
                            r.Status == "Pending"
                    );


            var rejectedCount =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .CountAsync(
                        r =>
                            r.ProgramId == id &&
                            r.Status == "Rejected"
                    );


            // ------------------------------------------------------
            // SEND DATA TO VIEW
            // ------------------------------------------------------

            ViewBag.Participants =
                participants;

            ViewBag.ParticipantCount =
                participants.Count;

            ViewBag.ApprovedCount =
                approvedCount;

            ViewBag.PendingCount =
                pendingCount;

            ViewBag.RejectedCount =
                rejectedCount;

            ViewBag.AvailableSlots =
                Math.Max(
                    0,
                    program.Capacity -
                    approvedCount
                );


            return View(program);
        }


        // ==========================================================
        // CREATE PROGRAM - GET
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public IActionResult Create()
        {
            // IMPORTANT:
            // The Create view uses Model.Status,
            // Model.Capacity, Model.Barangay, etc.
            //
            // Therefore the view must receive a YouthProgram object.
            // Returning View() would make Model == null.

            var program =
                new YouthProgram
                {
                    Name = string.Empty,

                    Description = string.Empty,

                    Schedule = string.Empty,

                    Status = "Active",

                    Capacity = 50,

                    Barangay = string.Empty,

                    CreatedAt = DateTime.UtcNow
                };

            return View(program);
        }


        // ==========================================================
        // CREATE PROGRAM - POST
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            YouthProgram program,
            DateTime? programDate,
            TimeOnly? scheduleStartTime,
            TimeOnly? scheduleEndTime)
        {
            // ------------------------------------------------------
            // PRESERVE TYPED SCHEDULE PICKER VALUES
            //
            // ScheduleStart/ScheduleEnd on the model are only set
            // when all three pickers bind together. When any one
            // of them is missing or invalid the model stays stale,
            // so stash the attempted values for the views to
            // re-render on every failed POST round-trip.
            // ------------------------------------------------------

            ViewBag.ProgramDate =
                programDate?.ToString("yyyy-MM-dd");

            ViewBag.ScheduleStartTime =
                scheduleStartTime?.ToString("HH:mm");

            ViewBag.ScheduleEndTime =
                scheduleEndTime?.ToString("HH:mm");


            // ------------------------------------------------------
            // SAFETY CHECK
            // ------------------------------------------------------

            if (program == null)
            {
                program =
                    new YouthProgram
                    {
                        Status = "Active",
                        Capacity = 50
                    };

                ModelState.AddModelError(
                    string.Empty,
                    "The program information is invalid."
                );

                return View(program);
            }


            // ------------------------------------------------------
            // NORMALIZE INPUT FIRST
            // ------------------------------------------------------

            program.Name =
                program.Name?.Trim()
                ?? string.Empty;

            program.Description =
                string.IsNullOrWhiteSpace(
                    program.Description)
                    ? null
                    : program.Description.Trim();

            program.Schedule =
                program.Schedule?.Trim()
                ?? string.Empty;

            program.Barangay =
                NormalizeBarangayForStorage(
                    program.Barangay
                );


            // ------------------------------------------------------
            // DEFAULT STATUS
            // ------------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    program.Status))
            {
                program.Status = "Active";
            }


            // ------------------------------------------------------
            // STRUCTURED PROGRAM SCHEDULE
            //
            // The Program Date and Program Start/End Time pickers
            // are the authoritative schedule for attendance
            // validation and automatic announcements. The free-text
            // Schedule field is a description only and is never
            // used to validate attendance.
            // ------------------------------------------------------

            if (!programDate.HasValue)
            {
                ModelState.AddModelError(
                    "ProgramDate",
                    "The program date is required."
                );
            }


            if (!scheduleStartTime.HasValue)
            {
                ModelState.AddModelError(
                    "ScheduleStartTime",
                    "The program start time is required."
                );
            }


            if (!scheduleEndTime.HasValue)
            {
                ModelState.AddModelError(
                    "ScheduleEndTime",
                    "The program end time is required."
                );
            }


            if (programDate.HasValue &&
                scheduleStartTime.HasValue &&
                scheduleEndTime.HasValue)
            {
                program.ScheduleStart =
                    programDate.Value.Date
                    + scheduleStartTime.Value.ToTimeSpan();

                program.ScheduleEnd =
                    programDate.Value.Date
                    + scheduleEndTime.Value.ToTimeSpan();


                // --------------------------------------------------
                // SCHEDULE TEXT AUTO-FILL
                //
                // The legacy Schedule column feeds the read-only
                // displays, dashboards, and search, but the
                // Create/Edit forms no longer offer an input for
                // it. When no text was posted, generate it from
                // the pickers. Existing descriptions are never
                // overwritten.
                // --------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                        program.Schedule))
                {
                    program.Schedule =
                        BuildScheduleText(
                            program.ScheduleStart,
                            program.ScheduleEnd
                        );
                }


                if (program.ScheduleEnd <= program.ScheduleStart)
                {
                    ModelState.AddModelError(
                        "ScheduleEndTime",
                        "The program end time must be after the program start time."
                    );
                }
            }


            // ------------------------------------------------------
            // ATTENDANCE WINDOW VALIDATION
            // Shared by Create and Edit: each of the four windows
            // must be ordered, the windows must not overlap, and
            // they must fit inside the overall program schedule.
            // ------------------------------------------------------

            ValidateAttendanceWindows(
                program,
                ModelState);

            ValidateWindowsAgainstSchedule(
                program,
                ModelState);


            // ------------------------------------------------------
            // VALIDATION
            // ------------------------------------------------------

            if (!ModelState.IsValid)
            {
                return View(program);
            }


            if (string.IsNullOrWhiteSpace(
                    program.Barangay))
            {
                ModelState.AddModelError(
                    nameof(program.Barangay),
                    "Barangay is required."
                );

                return View(program);
            }


            // ------------------------------------------------------
            // CREATED DATE
            // ------------------------------------------------------

            program.CreatedAt =
                DateTime.UtcNow;


            // ------------------------------------------------------
            // PERSIST EXACT-MIDNIGHT WINDOW VALUES ON INSERT
            //
            // The window columns carry database defaults (the
            // barangay default schedule) so rows that existed
            // before the windows feature received sensible times.
            // On INSERT, however, EF Core treats a value-generated
            // property that equals its CLR default -
            // TimeOnly.MinValue, exactly 00:00 - as "not
            // supplied": it replaces the entity value with the
            // configured default and stores that instead. A
            // program validated with a midnight boundary (e.g.
            // TimeInStart 00:00-23:58) would silently store 7:00,
            // breaking the submission window the admin picked.
            //
            // All eight boundaries and the sentinel flag are
            // therefore captured BEFORE SaveChanges (EF rewrites
            // the entity values during SaveChanges) and any
            // midnight boundary is written explicitly right after
            // the insert. Edits are unaffected: EF includes
            // changed columns in UPDATE statements.
            // ------------------------------------------------------

            var boundTimeInStart     = program.TimeInStart;
            var boundTimeInEnd       = program.TimeInEnd;
            var boundTimeOutStart    = program.TimeOutStart;
            var boundTimeOutEnd      = program.TimeOutEnd;
            var boundAfternoonInStart  =
                program.AfternoonInStart;
            var boundAfternoonInEnd    =
                program.AfternoonInEnd;
            var boundAfternoonOutStart =
                program.AfternoonOutStart;
            var boundAfternoonOutEnd   =
                program.AfternoonOutEnd;

            var hasMidnightBoundary =
                boundTimeInStart        == default ||
                boundTimeInEnd          == default ||
                boundTimeOutStart       == default ||
                boundTimeOutEnd         == default ||
                boundAfternoonInStart   == default ||
                boundAfternoonInEnd     == default ||
                boundAfternoonOutStart  == default ||
                boundAfternoonOutEnd    == default;


            // ------------------------------------------------------
            // SAVE PROGRAM
            // ------------------------------------------------------

            _context.YouthPrograms.Add(
                program
            );

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Nothing was persisted: re-render the form with
                // every attempted value intact.

                _logger.LogError(
                    ex,
                    "Failed to create program {ProgramName}.",
                    program.Name
                );

                ModelState.AddModelError(
                    string.Empty,
                    "The program could not be saved because of a temporary database problem. Please try again. Your entered values have been kept."
                );

                return View(program);
            }


            var scheduleWriteWarning = string.Empty;

            if (hasMidnightBoundary)
            {
                try
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE YouthPrograms SET TimeInStart = {0}, TimeInEnd = {1}, TimeOutStart = {2}, TimeOutEnd = {3}, AfternoonInStart = {4}, AfternoonInEnd = {5}, AfternoonOutStart = {6}, AfternoonOutEnd = {7} WHERE Id = {8}",
                        boundTimeInStart,
                        boundTimeInEnd,
                        boundTimeOutStart,
                        boundTimeOutEnd,
                        boundAfternoonInStart,
                        boundAfternoonInEnd,
                        boundAfternoonOutStart,
                        boundAfternoonOutEnd,
                        program.Id);
                }
                catch (Exception ex)
                {
                    // The row already exists here. Fail forward
                    // with an explicit warning instead of an
                    // error that would wrongly suggest the
                    // program was never saved.

                    _logger.LogError(
                        ex,
                        "Program {ProgramId} was created but exact-midnight attendance window values could not be written.",
                        program.Id
                    );

                    scheduleWriteWarning =
                        " Warning: exact-midnight attendance window values could not be verified - please review the attendance windows.";
                }
            }


            // ------------------------------------------------------
            // AUTOMATIC BARANGAY ASSIGNMENT
            // The program row is already committed; an assignment
            // failure must never look like a failed save.
            // ------------------------------------------------------

            AssignmentCounts? assignmentCounts = null;

            var assignmentWarning = string.Empty;

            try
            {
                assignmentCounts =
                    await AutomaticallyAssignYouthToProgram(
                        program.Id
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Program {ProgramId} was created but automatic Barangay assignment failed.",
                    program.Id
                );

                assignmentWarning =
                    " Automatic youth assignment could not be completed for a technical reason - use Manage Participants to assign participants manually.";
            }


            TempData["Success"] =
                (assignmentCounts != null
                    ? BuildAssignmentMessage(
                        "Program created successfully.",
                        assignmentCounts)
                    : "Program created successfully.")
                + scheduleWriteWarning
                + assignmentWarning;


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // EDIT PROGRAM - GET
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> Edit(
            int id)
        {
            var program =
                await _context.YouthPrograms
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );

            if (program == null)
            {
                return NotFound();
            }

            return View(program);
        }


        // ==========================================================
        // EDIT PROGRAM - POST
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            YouthProgram program,
            DateTime? programDate,
            TimeOnly? scheduleStartTime,
            TimeOnly? scheduleEndTime)
        {
            // ------------------------------------------------------
            // PRESERVE TYPED SCHEDULE PICKER VALUES
            //
            // On a failed POST the model's ScheduleStart/End only
            // exist when all three pickers bound together, so the
            // stale stored values would repaint the form. Stash
            // the attempted values for every failed round-trip.
            // ------------------------------------------------------

            ViewBag.ProgramDate =
                programDate?.ToString("yyyy-MM-dd");

            ViewBag.ScheduleStartTime =
                scheduleStartTime?.ToString("HH:mm");

            ViewBag.ScheduleEndTime =
                scheduleEndTime?.ToString("HH:mm");


            if (program == null)
            {
                return NotFound();
            }


            if (id != program.Id)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // NORMALIZE INPUT
            // ------------------------------------------------------

            program.Name =
                program.Name?.Trim()
                ?? string.Empty;

            program.Description =
                string.IsNullOrWhiteSpace(
                    program.Description)
                    ? null
                    : program.Description.Trim();

            program.Schedule =
                program.Schedule?.Trim()
                ?? string.Empty;

            program.Barangay =
                NormalizeBarangayForStorage(
                    program.Barangay
                );


            // ------------------------------------------------------
            // STRUCTURED PROGRAM SCHEDULE (SAME RULES AS CREATE)
            // The saved program values load into the pickers on
            // GET and remain the source of truth unless the admin
            // explicitly changes them here; nothing is reset to
            // defaults on load.
            // ------------------------------------------------------

            if (!programDate.HasValue)
            {
                ModelState.AddModelError(
                    "ProgramDate",
                    "The program date is required."
                );
            }


            if (!scheduleStartTime.HasValue)
            {
                ModelState.AddModelError(
                    "ScheduleStartTime",
                    "The program start time is required."
                );
            }


            if (!scheduleEndTime.HasValue)
            {
                ModelState.AddModelError(
                    "ScheduleEndTime",
                    "The program end time is required."
                );
            }


            if (programDate.HasValue &&
                scheduleStartTime.HasValue &&
                scheduleEndTime.HasValue)
            {
                program.ScheduleStart =
                    programDate.Value.Date
                    + scheduleStartTime.Value.ToTimeSpan();

                program.ScheduleEnd =
                    programDate.Value.Date
                    + scheduleEndTime.Value.ToTimeSpan();


                // --------------------------------------------------
                // SCHEDULE TEXT AUTO-FILL
                //
                // The legacy Schedule column feeds the read-only
                // displays, dashboards, and search, but the
                // Create/Edit forms no longer offer an input for
                // it. When no text was posted, generate it from
                // the pickers. Existing descriptions are never
                // overwritten.
                // --------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                        program.Schedule))
                {
                    program.Schedule =
                        BuildScheduleText(
                            program.ScheduleStart,
                            program.ScheduleEnd
                        );
                }


                if (program.ScheduleEnd <= program.ScheduleStart)
                {
                    ModelState.AddModelError(
                        "ScheduleEndTime",
                        "The program end time must be after the program start time."
                    );
                }
            }


            // ------------------------------------------------------
            // ATTENDANCE WINDOW VALIDATION
            // Shared by Create and Edit: each of the four windows
            // must be ordered, the windows must not overlap, and
            // they must fit inside the overall program schedule.
            // ------------------------------------------------------

            ValidateAttendanceWindows(
                program,
                ModelState);

            ValidateWindowsAgainstSchedule(
                program,
                ModelState);


            // ------------------------------------------------------
            // VALIDATION
            // ------------------------------------------------------

            if (!ModelState.IsValid)
            {
                return View(program);
            }


            if (string.IsNullOrWhiteSpace(
                    program.Barangay))
            {
                ModelState.AddModelError(
                    nameof(program.Barangay),
                    "Barangay is required."
                );

                return View(program);
            }


            // ------------------------------------------------------
            // FIND EXISTING PROGRAM
            // ------------------------------------------------------

            var existingProgram =
                await _context.YouthPrograms
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );

            if (existingProgram == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // UPDATE FIELDS
            // ------------------------------------------------------

            existingProgram.Name =
                program.Name;

            existingProgram.Description =
                program.Description;

            existingProgram.Schedule =
                program.Schedule;

            existingProgram.ScheduleStart =
                program.ScheduleStart;

            existingProgram.ScheduleEnd =
                program.ScheduleEnd;

            existingProgram.TimeInStart =
                program.TimeInStart;

            existingProgram.TimeInEnd =
                program.TimeInEnd;

            existingProgram.TimeOutStart =
                program.TimeOutStart;

            existingProgram.TimeOutEnd =
                program.TimeOutEnd;

            existingProgram.AfternoonInStart =
                program.AfternoonInStart;

            existingProgram.AfternoonInEnd =
                program.AfternoonInEnd;

            existingProgram.AfternoonOutStart =
                program.AfternoonOutStart;

            existingProgram.AfternoonOutEnd =
                program.AfternoonOutEnd;

            existingProgram.Status =
                string.IsNullOrWhiteSpace(
                    program.Status)
                    ? "Active"
                    : program.Status;

            existingProgram.Capacity =
                program.Capacity;

            existingProgram.Barangay =
                program.Barangay;


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Nothing was persisted: re-render the posted
                // values so the admin can retry safely.

                _logger.LogError(
                    ex,
                    "Failed to update program {ProgramId} ({ProgramName}).",
                    id,
                    existingProgram.Name
                );

                ModelState.AddModelError(
                    string.Empty,
                    "The program could not be saved because of a temporary database problem. Please try again. Your entered values have been kept."
                );

                return View(program);
            }


            // ------------------------------------------------------
            // AUTOMATICALLY ASSIGN NEW MATCHING YOUTH
            // The update is already committed; an assignment
            // failure must never look like a failed save.
            // ------------------------------------------------------

            AssignmentCounts? updateCounts = null;

            var updateAssignmentWarning = string.Empty;

            try
            {
                updateCounts =
                    await AutomaticallyAssignYouthToProgram(
                        id
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Program {ProgramId} was updated but automatic Barangay assignment failed.",
                    id
                );

                updateAssignmentWarning =
                    " Automatic youth assignment could not be completed for a technical reason - use Manage Participants to assign participants manually.";
            }


            TempData["Success"] =
                (updateCounts != null
                    ? BuildAssignmentMessage(
                        "Program updated successfully.",
                        updateCounts)
                    : "Program updated successfully.")
                + updateAssignmentWarning;


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // AUTOMATIC BARANGAY ASSIGNMENT
        // ==========================================================
        //
        // YouthParticipant.Barangay
        //          ==
        // YouthProgram.Barangay
        //
        // Matching youth are automatically assigned through
        // ProgramRegistration.
        //
        // Automatic assignment creates:
        //
        // YouthParticipant
        //       ↓
        // ProgramRegistration (Pending)
        //       ↓
        // Admin / Staff approval in Registration
        //
        // Automatic matching only creates the registration
        // REQUEST. The status stays "Pending" until an Admin or
        // authorized Staff member processes it, so automatic
        // matching never bypasses the approval workflow.
        //
        // Only an "Approved" registration is eligible for
        // attendance recording.
        //
        // Attendance is NOT created here.
        // Attendance is recorded later using RegistrationId.
        //
        // CONCURRENCY: the whole matching + creation pass runs
        // inside one transaction that first locks the program row
        // (SELECT ... FOR UPDATE) and then re-reads everything it
        // needs. Two simultaneous assignment requests therefore
        // serialize: the second one re-reads fresh registrations
        // and cannot create duplicate rows or push past capacity.
        // ==========================================================

        private async Task<AssignmentCounts>
            AutomaticallyAssignYouthToProgram(
                int programId)
        {
            var counts =
                new AssignmentCounts();


            // --------------------------------------------------
            // SERIALIZE ASSIGNMENTS PER PROGRAM
            // The program row is locked before anything is read,
            // so a concurrent Create/Edit/AssignMatchingYouth for
            // the same program waits here.
            // --------------------------------------------------

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();


            await _context.Database.ExecuteSqlRawAsync(
                "SELECT Id FROM YouthPrograms WHERE Id = {0} FOR UPDATE",
                programId);


            // Re-read AFTER acquiring the lock so this request
            // always works on the program state the concurrent
            // request committed.

            var program =
                await _context.YouthPrograms
                    .FirstOrDefaultAsync(
                        p => p.Id == programId
                    );

            if (program == null)
            {
                return counts;
            }


            if (string.IsNullOrWhiteSpace(
                    program.Barangay))
            {
                return counts;
            }


            var programBarangay =
                BarangayNormalizer.ForComparison(
                    program.Barangay
                );


            if (string.IsNullOrWhiteSpace(
                    programBarangay))
            {
                return counts;
            }


            // ------------------------------------------------------
            // GET ACTIVE YOUTH
            //
            // We normalize in C# so different inputs such as:
            //
            // "San Jose"
            // "Barangay San Jose"
            // " barangay san jose "
            //
            // can still match.
            // ------------------------------------------------------

            var activeYouth =
                await _context.YouthParticipants
                    .Where(
                        y => y.IsActive
                    )
                    .ToListAsync();


            var matchingYouth =
                activeYouth
                    .Where(
                        y =>
                            BarangayNormalizer.ForComparison(
                                y.Barangay
                            )
                            ==
                            programBarangay
                    )
                    .ToList();


            counts.MatchedCount =
                matchingYouth.Count;

            if (!matchingYouth.Any())
            {
                return counts;
            }


            // ------------------------------------------------------
            // EXISTING REGISTRATIONS
            //
            // Also load the linked participant so legacy rows that
            // were recorded before YouthParticipantId was filled in
            // can still be recognized through the same Identity user.
            // ------------------------------------------------------

            var existingRegistrations =
                await _context.ProgramRegistrations
                    .Include(
                        r =>
                            r.YouthParticipant
                    )
                    .Where(
                        r =>
                            r.ProgramId ==
                            programId
                    )
                    .ToListAsync();


            // ------------------------------------------------------
            // ASSIGN EACH MATCHING YOUTH
            //
            // confirmedCount is capacity tracked over Approved +
            // Pending registrations: a Pending request already
            // occupies a seat for this assignment pass, so a
            // repeated run can never stack unbounded Pending rows
            // past the configured capacity.
            // ------------------------------------------------------

            var createdRegistrations =
                new List<ProgramRegistration>();

            foreach (var youth in matchingYouth)
            {
                var existingRegistration =
                    existingRegistrations
                        .FirstOrDefault(
                            r =>
                                r.YouthParticipantId
                                ==
                                youth.Id

                                ||

                                (
                                    youth.UserId !=
                                        null

                                    &&

                                    (
                                        r.UserId ==
                                            youth.UserId

                                        ||

                                        (
                                            r.YouthParticipant !=
                                                null

                                            &&

                                            r.YouthParticipant.UserId
                                            ==
                                            youth.UserId
                                        )
                                    )
                                )
                        );


                // --------------------------------------------------
                // PREVENT DUPLICATE REGISTRATION
                // --------------------------------------------------

                if (existingRegistration != null)
                {
                    counts.AlreadyRegisteredCount++;

                    continue;
                }


                // --------------------------------------------------
                // CAPACITY CHECK
                // --------------------------------------------------

                var committedSeats =
                    existingRegistrations
                        .Count(
                            r =>
                                r.Status ==
                                    "Approved"

                                ||

                                r.Status ==
                                    "Pending"
                        );


                if (committedSeats >=
                    program.Capacity)
                {
                    counts.AtCapacityCount++;

                    break;
                }


                // --------------------------------------------------
                // CREATE PENDING REGISTRATION
                //
                // The automatic match creates a registration
                // request only. Approval happens later through
                // Registration (Admin / Staff).
                // --------------------------------------------------

                var registration =
                    new ProgramRegistration
                    {
                        ProgramId =
                            program.Id,

                        YouthParticipantId =
                            youth.Id,

                        UserId =
                            youth.UserId,

                        UserEmail =
                            null,

                        Status =
                            "Pending",

                        RegisteredAt =
                            DateTime.UtcNow
                    };


                _context.ProgramRegistrations
                    .Add(
                        registration
                    );


                existingRegistrations
                    .Add(
                        registration
                    );

                createdRegistrations
                    .Add(
                        registration
                    );
            }


            counts.AssignedCount =
                createdRegistrations.Count;


            if (!createdRegistrations.Any())
            {
                return counts;
            }


            // --------------------------------------------------
            // COMMIT REGISTRATIONS INSIDE THE SAME TRANSACTION
            // The row lock plus the fresh registration read above
            // already serialized concurrent assignments; a single
            // SaveChanges here means all matched rows commit
            // together or not at all.
            // --------------------------------------------------

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();


            // ------------------------------------------------------
            // NOTIFICATION FOR AUTO-CREATED REGISTRATION REQUESTS
            //
            // One notification per genuinely NEW pending request for
            // youth with a linked Identity account. Pre-existing or
            // unchanged registrations are never notified again.
            // ------------------------------------------------------

            foreach (var createdRegistration in createdRegistrations)
            {
                var autoNotification =
                    NotificationHelper.ForRegistration(
                        createdRegistration,
                        "Registration Request Submitted",
                        $"You have been automatically matched to \"{program.Name}\". Your registration is waiting for approval.",
                        "Registration");


                if (autoNotification == null)
                {
                    continue;
                }


                _context.YouthNotifications
                    .Add(
                        autoNotification
                    );
            }


            if (createdRegistrations.Any())
            {
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // A notification problem must not undo
                    // successful registration requests - but it
                    // must never be silent either.

                    counts.NotificationFailed = true;

                    _logger.LogWarning(
                        ex,
                        "Registration requests were created for program {ProgramId} but notification persistence failed.",
                        program.Id
                    );
                }
            }


            return counts;
        }


        // ==========================================================
        // MANAGE PROGRAM PARTICIPANTS
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult>
            ManageParticipants(
                int id)
        {
            var program =
                await _context.YouthPrograms
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );

            if (program == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // NOTE: Automatic barangay assignment is intentionally
            // NOT run here. This is a GET action and must not write
            // to the database. Assignment is triggered explicitly
            // through the AssignMatchingYouth POST action.
            // ------------------------------------------------------


            // ------------------------------------------------------
            // GET ACTIVE YOUTH
            // ------------------------------------------------------

            var participants =
                await _context.YouthParticipants
                    .Where(
                        y => y.IsActive
                    )
                    .OrderBy(
                        y => y.LastName
                    )
                    .ThenBy(
                        y => y.FirstName
                    )
                    .ToListAsync();


            // ------------------------------------------------------
            // GET PROGRAM REGISTRATIONS
            // ------------------------------------------------------

            var registrations =
                await _context.ProgramRegistrations
                    .Where(
                        r =>
                            r.ProgramId == id &&
                            r.YouthParticipantId
                                .HasValue
                    )
                    .ToListAsync();


            ViewBag.Program =
                program;


            // ------------------------------------------------------
            // APPROVED PARTICIPANT IDS
            // ------------------------------------------------------

            ViewBag.ApprovedParticipantIds =
                registrations
                    .Where(
                        r =>
                            r.Status ==
                            "Approved"
                    )
                    .Select(
                        r =>
                            r.YouthParticipantId!
                                .Value
                    )
                    .ToHashSet();


            // ------------------------------------------------------
            // PENDING PARTICIPANT IDS
            // ------------------------------------------------------

            ViewBag.PendingParticipantIds =
                registrations
                    .Where(
                        r =>
                            r.Status ==
                            "Pending"
                    )
                    .Select(
                        r =>
                            r.YouthParticipantId!
                                .Value
                    )
                    .ToHashSet();


            // ------------------------------------------------------
            // AVAILABLE SLOTS
            // ------------------------------------------------------

            var approvedIds =
                ViewBag.ApprovedParticipantIds
                    as HashSet<int>
                ?? new HashSet<int>();

            var pendingIds =
                ViewBag.PendingParticipantIds
                    as HashSet<int>
                ?? new HashSet<int>();

            var approvedCount =
                registrations
                    .Count(
                        r =>
                            r.Status ==
                            "Approved"
                    );


            ViewBag.ApprovedCount =
                approvedCount;

            ViewBag.AvailableSlots =
                Math.Max(
                    0,
                    program.Capacity -
                    approvedCount
                );


            // --------------------------------------------------
            // BARANGAY MATCH PREVIEW
            // Matched = active participants whose Barangay matches
            // the program's Barangay through the shared canonical
            // normalizer, BEFORE any registration exists. The
            // view shows the pending/approved split of that
            // match set so the Admin can preview the effect of
            // "Assign Matching Youth" without any state change.
            // --------------------------------------------------

            var programBarangay =
                BarangayNormalizer.ForComparison(
                    program.Barangay
                );

            if (!string.IsNullOrWhiteSpace(
                    programBarangay))
            {
                var matchedIds =
                    participants
                        .Where(
                            y =>
                                BarangayNormalizer
                                    .ForComparison(
                                        y.Barangay
                                    )
                                ==
                                programBarangay
                        )
                        .Select(y => y.Id)
                        .ToHashSet();


                ViewBag.MatchedCount =
                    matchedIds.Count;

                ViewBag.MatchedApprovedCount =
                    matchedIds
                        .Count(
                            approvedIds
                                .Contains
                        );

                ViewBag.MatchedPendingCount =
                    matchedIds
                        .Count(
                            pendingIds
                                .Contains
                        );

                ViewBag.MatchedWithoutApprovalCount =
                    matchedIds.Count
                    - ViewBag.MatchedApprovedCount;
            }
            else
            {
                ViewBag.MatchedCount = 0;

                ViewBag.MatchedApprovedCount = 0;

                ViewBag.MatchedPendingCount = 0;

                ViewBag.MatchedWithoutApprovalCount = 0;
            }


            return View(participants);
        }


        // ==========================================================
        // ASSIGN MATCHING YOUTH (EXPLICIT ACTION)
        // ==========================================================
        //
        // Automatic barangay assignment used to run inside the
        // ManageParticipants GET action, which meant simply viewing
        // the page wrote new Pending registrations to the database.
        // GET requests must not change state, so assignment is now
        // triggered only when an authorized user submits this form.
        //
        // The assignment itself still only creates "Pending"
        // registration requests. Approval continues to happen in
        // the Registration module.
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            AssignMatchingYouth(
                int id)
        {
            var program =
                await _context.YouthPrograms
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );

            if (program == null)
            {
                return NotFound();
            }


            var assignCounts =
                await AutomaticallyAssignYouthToProgram(
                    id
                );


            TempData["Success"] =
                BuildAssignmentMessage(
                    "Matching youth were assigned to this program.",
                    assignCounts);


            return RedirectToAction(
                nameof(ManageParticipants),
                new
                {
                    id = program.Id
                }
            );
        }


        // ==========================================================
        // SET PROGRAM STATUS
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            SetStatus(
                int id,
                string status)
        {
            var program =
                await _context.YouthPrograms
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );

            if (program == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // ONLY VALID STATUSES
            // 
            // Added "Completed" to allow marking programs as finished
            // for certificate issuance eligibility.
            // ======================================================

            if (status != "Active" &&
                status != "Inactive" &&
                status != "Completed")
            {
                TempData["Error"] =
                    "Invalid program status.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            program.Status =
                status;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"Program status changed to {status}.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // DELETE CONFIRMATION (GET)
        // ==========================================================
        //
        // Admin only. Renders the existing confirmation page that
        // the Program Details "Delete" link points to. A GET only
        // displays the prompt - it never removes anything. Only
        // the POST action below deletes.
        // ==========================================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> DeleteConfirmation(
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


            return View(
                "Delete",
                program
            );
        }


        // ==========================================================
        // DELETE PROGRAM PERMANENTLY
        // ==========================================================
        //
        // Admin only, POST + antiforgery only.
        //
        // Removes an obsolete program together with the records
        // that belong exclusively to that program:
        //
        // Program
        //   ↓
        // Registrations (program scoped)
        //   ↓
        // Attendance records      ─┐
        // Photo submissions       ─┤ by registration
        // Certificates            ─┘
        // Program notifications (program/registration scoped)
        // Announcement logs (program scoped - idempotency rows)
        //
        // Participant rows and Identity accounts are never
        // touched: a program delete removes registrations, not
        // the people who held them.
        //
        // Everything runs in ONE transaction after locking the
        // program row (SELECT ... FOR UPDATE), so concurrent
        // delete/edit requests serialize and a failure can never
        // leave a partially deleted program. Stored photo files
        // are removed only AFTER the commit, only when no
        // surviving submission still references the file, and
        // only through AttendancePhotoStorage, which confines the
        // resolved path to the configured photo folder.
        // ==========================================================

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            Delete(
                int id)
        {
            // ------------------------------------------------------
            // RETRY ANY PHOTO FILE LEFT OVER FROM AN EARLIER DELETE
            //
            // A file whose removal failed after a previous commit
            // is queued; retry it before doing new work.
            // ------------------------------------------------------

            try
            {
                var stillPending =
                    _photoStorage.RetryPendingDeletes();

                if (stillPending > 0)
                {
                    _logger.LogWarning(
                        "{Count} attendance photo file(s) remain queued for deletion.",
                        stillPending);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Pending attendance photo cleanup retry failed.");
            }


            var program =
                default(YouthProgram);

            var registrations =
                new List<ProgramRegistration>();

            var attendance =
                new List<AttendanceRecord>();

            var photos =
                new List<AttendancePhotoSubmission>();

            var certificates =
                new List<Certificate>();

            var announcementLogs =
                new List<AttendanceAnnouncementLog>();

            var notifications =
                new List<YouthNotification>();

            var exclusiveFileNames =
                new List<string>();


            try
            {
                await using var transaction =
                    await _context.Database
                        .BeginTransactionAsync();


                // --------------------------------------------------
                // LOCK THE TARGET ROW
                //
                // (SELECT ... FOR UPDATE) so a concurrent delete
                // or status change on the same program waits for
                // this transaction and then re-reads the result.
                // --------------------------------------------------

                await _context.Database.ExecuteSqlRawAsync(
                    "SELECT Id FROM YouthPrograms WHERE Id = {0} FOR UPDATE",
                    id);


                program =
                    await _context.YouthPrograms
                        .FirstOrDefaultAsync(
                            p => p.Id == id
                        );

                if (program == null)
                {
                    // Already deleted by a concurrent request.
                    // Disposing the transaction rolls back.

                    return NotFound();
                }


                // --------------------------------------------------
                // LOAD EVERYTHING THAT BELONGS TO THIS PROGRAM
                // --------------------------------------------------

                registrations =
                    await _context.ProgramRegistrations
                        .Where(
                            r =>
                                r.ProgramId ==
                                id
                        )
                        .ToListAsync();


                var registrationIds =
                    registrations
                        .Select(r => r.Id)
                        .ToList();


                photos =
                    await _context.AttendancePhotoSubmissions
                        .Where(
                            s =>
                                registrationIds
                                    .Contains(
                                        s.RegistrationId
                                    )
                        )
                        .ToListAsync();


                certificates =
                    await _context.Certificates
                        .Where(
                            c =>
                                registrationIds
                                    .Contains(
                                        c.RegistrationId
                                    )
                        )
                        .ToListAsync();


                attendance =
                    await _context.AttendanceRecords
                        .Where(
                            a =>
                                registrationIds
                                    .Contains(
                                        a.RegistrationId
                                    )
                        )
                        .ToListAsync();


                announcementLogs =
                    await _context.AttendanceAnnouncementLogs
                        .Where(
                            l =>
                                l.ProgramId ==
                                id
                        )
                        .ToListAsync();


                notifications =
                    await _context.YouthNotifications
                        .Where(
                            n =>
                                n.ProgramId == id

                                ||

                                (
                                    n.RegistrationId != null

                                    &&

                                    registrationIds
                                        .Contains(
                                            n.RegistrationId
                                                .Value
                                        )
                                )
                        )
                        .ToListAsync();


                // --------------------------------------------------
                // FILES OWNED EXCLUSIVELY BY THIS PROGRAM
                //
                // A file name referenced by any submission OUTSIDE
                // the deleted registrations must survive, so it is
                // excluded from cleanup. Checked inside the same
                // transaction, before anything is removed.
                // --------------------------------------------------

                var allFileNames =
                    photos
                        .Select(s => s.FileName)
                        .Distinct()
                        .ToList();

                var sharedFileNames =
                    allFileNames.Count == 0
                        ? new List<string>()
                        : await _context.AttendancePhotoSubmissions
                            .Where(
                                s =>
                                    allFileNames
                                        .Contains(
                                            s.FileName
                                        )

                                    &&

                                    !registrationIds
                                        .Contains(
                                            s.RegistrationId
                                        )
                            )
                            .Select(s => s.FileName)
                            .Distinct()
                            .ToListAsync();

                exclusiveFileNames =
                    allFileNames
                        .Where(
                            fn =>
                                !sharedFileNames
                                    .Contains(fn)
                        )
                        .ToList();


                // --------------------------------------------------
                // DELETE CHILDREN BEFORE THE PARENT
                //
                // One SaveChanges inside the one transaction.
                // --------------------------------------------------

                if (photos.Count > 0)
                {
                    _context.AttendancePhotoSubmissions
                        .RemoveRange(photos);
                }

                if (certificates.Count > 0)
                {
                    _context.Certificates
                        .RemoveRange(certificates);
                }

                if (attendance.Count > 0)
                {
                    _context.AttendanceRecords
                        .RemoveRange(attendance);
                }

                if (announcementLogs.Count > 0)
                {
                    _context.AttendanceAnnouncementLogs
                        .RemoveRange(announcementLogs);
                }

                if (notifications.Count > 0)
                {
                    _context.YouthNotifications
                        .RemoveRange(notifications);
                }

                if (registrations.Count > 0)
                {
                    _context.ProgramRegistrations
                        .RemoveRange(registrations);
                }


                _context.YouthPrograms
                    .Remove(program);


                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Program {ProgramId} delete hit a concurrency conflict; nothing was committed.",
                    id);

                TempData["Error"] =
                    "Could not delete: the program changed while processing or was already deleted. " +
                    "Refresh the list and try again.";

                return RedirectToAction(
                    nameof(Index)
                );
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Program {ProgramId} delete failed at the database; transaction rolled back.",
                    id);

                TempData["Error"] =
                    "Could not delete: related records could not be removed safely. " +
                    "No changes were made. Refresh the list and try again.";

                return RedirectToAction(
                    nameof(Index)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Program {ProgramId} delete failed; transaction rolled back.",
                    id);

                TempData["Error"] =
                    "Could not delete the program due to a database error. " +
                    "No changes were made. Please try again.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // ------------------------------------------------------
            // FILE CLEANUP (AFTER COMMIT - BEST EFFORT)
            //
            // The database deletion is already durable. A file
            // that cannot be removed now is logged and queued for
            // retry on the next delete operation; it never fails
            // the request or corrupts the committed deletion.
            // ------------------------------------------------------

            var cleanupFailures = 0;

            foreach (var fileName in exclusiveFileNames)
            {
                try
                {
                    _photoStorage.Delete(fileName);
                }
                catch (Exception ex)
                {
                    cleanupFailures++;

                    _logger.LogWarning(
                        ex,
                        "Could not delete attendance photo file {FileName} after deleting program {ProgramId}. Queued for retry.",
                        fileName,
                        id);

                    _photoStorage.EnqueuePendingDelete(fileName);
                }
            }


            var message =
                $"Program '{program.Name}' permanently deleted together with " +
                $"{registrations.Count} registration(s), " +
                $"{attendance.Count} attendance record(s), " +
                $"{photos.Count} photo submission(s), " +
                $"{certificates.Count} certificate(s), and " +
                $"{notifications.Count} related notification(s).";

            if (cleanupFailures > 0)
            {
                message +=
                    $" {cleanupFailures} stored photo file(s) could not be removed yet " +
                    "and are queued for automatic retry.";
            }

            TempData["Success"] = message;


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // SCHEDULE TEXT BUILDER (PHASE 6)
        // ==========================================================
        //
        // Builds a readable free-text schedule description from
        // the structured date/time pickers. Used only to auto-fill
        // a blank Schedule field; existing descriptions are never
        // overwritten.
        // ==========================================================

        private static string
            BuildScheduleText(
                DateTime? start,
                DateTime? end)
        {
            if (start.HasValue &&
                end.HasValue)
            {
                return $"From {start.Value:MMMM d, yyyy h:mm tt} to {end.Value:MMMM d, yyyy h:mm tt}";
            }


            if (start.HasValue)
            {
                return $"Starts {start.Value:MMMM d, yyyy h:mm tt}";
            }


            if (end.HasValue)
            {
                return $"Ends {end.Value:MMMM d, yyyy h:mm tt}";
            }


            return string.Empty;
        }


        // ==========================================================
        // ATTENDANCE WINDOW VALIDATION
        // ==========================================================
        //
        // Each of the four windows (Morning In/Out, Afternoon
        // In/Out) must be ordered (start strictly before end) and
        // consecutive windows must not overlap: Morning In ends
        // at or before Morning Out starts, Morning Out ends at or
        // before Afternoon In starts, and Afternoon In ends at or
        // before Afternoon Out starts. Deliberate same-moment
        // hand-offs are allowed; genuine overlaps are rejected.
        // ==========================================================

        private static void ValidateAttendanceWindows(
            YouthProgram program,
            ModelStateDictionary modelState)
        {
            if (program.TimeInStart >= program.TimeInEnd)
            {
                modelState.AddModelError(
                    nameof(program.TimeInEnd),
                    "The Morning Time In window end must be after its start."
                );
            }


            if (program.TimeOutStart >= program.TimeOutEnd)
            {
                modelState.AddModelError(
                    nameof(program.TimeOutEnd),
                    "The Morning Time Out window end must be after its start."
                );
            }


            if (program.AfternoonInStart >=
                program.AfternoonInEnd)
            {
                modelState.AddModelError(
                    nameof(program.AfternoonInEnd),
                    "The Afternoon Time In window end must be after its start."
                );
            }


            if (program.AfternoonOutStart >=
                program.AfternoonOutEnd)
            {
                modelState.AddModelError(
                    nameof(program.AfternoonOutEnd),
                    "The Afternoon Time Out window end must be after its start."
                );
            }


            if (program.TimeInEnd > program.TimeOutStart)
            {
                modelState.AddModelError(
                    nameof(program.TimeOutStart),
                    "The Morning Time Out window must start after the Morning Time In window ends."
                );
            }


            if (program.TimeOutEnd >
                program.AfternoonInStart)
            {
                modelState.AddModelError(
                    nameof(program.AfternoonInStart),
                    "The Afternoon Time In window must start after the Morning Time Out window ends."
                );
            }


            if (program.AfternoonInEnd >
                program.AfternoonOutStart)
            {
                modelState.AddModelError(
                    nameof(program.AfternoonOutStart),
                    "The Afternoon Time Out window must start after the Afternoon Time In window ends."
                );
            }
        }


        // ==========================================================
        // ATTENDANCE WINDOWS VS SCHEDULE COMPATIBILITY
        // ==========================================================
        //
        // When the schedule is a single day, the overall program
        // Start/End times must encompass every attendance window:
        // a window that opens before the program starts or ends
        // after it ends is a conflicting configuration and is
        // rejected up front.
        // ==========================================================

        private static void ValidateWindowsAgainstSchedule(
            YouthProgram program,
            ModelStateDictionary modelState)
        {
            if (!program.ScheduleStart.HasValue ||
                !program.ScheduleEnd.HasValue ||
                program.ScheduleStart.Value.Date !=
                    program.ScheduleEnd.Value.Date)
            {
                // Multi-day (or dateless) schedule: windows are
                // evaluated per scheduled day, nothing to check.
                return;
            }


            var scheduledEnd =
                TimeOnly.FromDateTime(
                    program.ScheduleEnd.Value);


            var checks = new (TimeOnly End, string Key,
                string Message)[]
            {
                (program.TimeInEnd,
                    nameof(program.TimeInStart),
                    "The Morning Time In window must end within the overall program schedule."),
                (program.TimeOutEnd,
                    nameof(program.TimeOutStart),
                    "The Morning Time Out window must end within the overall program schedule."),
                (program.AfternoonInEnd,
                    nameof(program.AfternoonInStart),
                    "The Afternoon Time In window must end within the overall program schedule."),
                (program.AfternoonOutEnd,
                    nameof(program.AfternoonOutStart),
                    "The Afternoon Time Out window must end within the overall program schedule.")
            };


            foreach (var check in checks)
            {
                if (check.End > scheduledEnd)
                {
                    modelState.AddModelError(
                        check.Key,
                        check.Message);
                }
            }


            // --------------------------------------------------
            // START SIDE: no window may open before the overall
            // program start time.
            // --------------------------------------------------

            var scheduledStart =
                TimeOnly.FromDateTime(
                    program.ScheduleStart.Value);


            var startChecks = new (TimeOnly Start, string Key,
                string Message)[]
            {
                (program.TimeInStart,
                    nameof(program.TimeInStart),
                    "The Morning Time In window must start within the overall program schedule."),
                (program.TimeOutStart,
                    nameof(program.TimeOutStart),
                    "The Morning Time Out window must start within the overall program schedule."),
                (program.AfternoonInStart,
                    nameof(program.AfternoonInStart),
                    "The Afternoon Time In window must start within the overall program schedule."),
                (program.AfternoonOutStart,
                    nameof(program.AfternoonOutStart),
                    "The Afternoon Time Out window must start within the overall program schedule.")
            };


            foreach (var check in startChecks)
            {
                if (check.Start < scheduledStart)
                {
                    modelState.AddModelError(
                        check.Key,
                        check.Message);
                }
            }
        }


        // ==========================================================
        // ASSIGNMENT COUNTS
        // ==========================================================
        //
        // Accurate feedback for the Match Barangay / Assign Youth
        // steps: how many active participants matched, how many
        // already had ANY registration row for the program
        // (Pending or Approved - never duplicated), how many were
        // skipped because the program had no room left, and how
        // many new Pending requests were actually created.
        // ==========================================================

        private sealed class AssignmentCounts
        {
            public int MatchedCount { get; set; }

            public int AlreadyRegisteredCount { get; set; }

            public int AssignedCount { get; set; }

            public int AtCapacityCount { get; set; }

            // True when registrations were committed but the
            // follow-up notification save failed.

            public bool NotificationFailed { get; set; }
        }


        private static string BuildAssignmentMessage(
            string prefix,
            AssignmentCounts counts)
        {
            if (counts == null ||
                counts.MatchedCount == 0)
            {
                return prefix
                    + " No active youth participants currently match this program's Barangay.";
            }


            var message =
                prefix
                + " "
                + counts.MatchedCount
                + " matching active participant(s): ";

            if (counts.AssignedCount > 0)
            {
                message +=
                    counts.AssignedCount
                    + " new registration request(s) created (Pending approval)";
            }

            if (counts.AlreadyRegisteredCount > 0)
            {
                if (counts.AssignedCount > 0)
                {
                    message += ", ";
                }

                message +=
                    counts.AlreadyRegisteredCount
                    + " already registered (skipped)";
            }

            if (counts.AtCapacityCount > 0)
            {
                if (counts.AssignedCount > 0 ||
                    counts.AlreadyRegisteredCount > 0)
                {
                    message += ", ";
                }

                message +=
                    counts.AtCapacityCount
                    + " skipped - program at capacity";
            }

            if (counts.AssignedCount == 0 &&
                counts.AlreadyRegisteredCount == 0 &&
                counts.AtCapacityCount == 0)
            {
                message += "all were already covered by existing registrations";
            }


            var finalMessage = message + ".";

            if (counts.NotificationFailed)
            {
                finalMessage +=
                    " Registration requests were saved, but automatic notifications could not be sent.";
            }

            return finalMessage;
        }


        // ==========================================================
        // BARANGAY STORAGE NORMALIZER
        // ==========================================================
        //
        // Keep the user's readable barangay value for display.
        // Only trim unnecessary spaces.
        //
        // Example:
        //
        // "  Barangay San Jose  "
        // becomes
        // "Barangay San Jose"
        // ==========================================================

        private static string
            NormalizeBarangayForStorage(
                string? barangay) =>
            BarangayNormalizer.ForStorage(
                barangay
            );


        // ==========================================================
        // BARANGAY COMPARISON NORMALIZER
        // ==========================================================
        //
        // This is only for comparison.
        //
        // Examples:
        //
        // "Barangay San Jose"
        // "San Jose"
        // " barangay san jose "
        //
        // all become:
        //
        // "san jose"
        // ==========================================================

        private static string
            NormalizeBarangayForComparison(
                string? barangay) =>
            BarangayNormalizer.ForComparison(
                barangay
            );
    }
}
