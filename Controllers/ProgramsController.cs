using BYPManagementSystem.Data;
using BYPManagementSystem.Models;
using BYPManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace BYPManagementSystem.Controllers
{
    [Authorize]
    public class ProgramsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProgramsController(ApplicationDbContext context)
        {
            _context = context;
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
            YouthProgram program)
        {
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
            // SCHEDULE TEXT AUTO-FILL (PHASE 6)
            //
            // The legacy Schedule field remains the free-text
            // description. When it is left blank but the structured
            // start/end date-times were picked, generate the text
            // once so the same information is never typed twice.
            // Existing descriptions are never overwritten.
            // ------------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    program.Schedule) &&
                (program.ScheduleStart.HasValue ||
                 program.ScheduleEnd.HasValue))
            {
                program.Schedule =
                    BuildScheduleText(
                        program.ScheduleStart,
                        program.ScheduleEnd
                    );


                // Clear the [Required] binding error so the
                // generated text is accepted.
                ModelState.Remove(
                    nameof(program.Schedule)
                );
            }


            // ------------------------------------------------------
            // DEFAULT STATUS
            // ------------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    program.Status))
            {
                program.Status = "Active";
            }


            // ------------------------------------------------------
            // SCHEDULE DATE/TIME VALIDATION
            //
            // The end date/time must be after the start date/time
            // when both pickers were used.
            // ------------------------------------------------------

            if (program.ScheduleStart.HasValue &&
                program.ScheduleEnd.HasValue &&
                program.ScheduleEnd <= program.ScheduleStart)
            {
                ModelState.AddModelError(
                    nameof(program.ScheduleEnd),
                    "The schedule end date/time must be after the start date/time."
                );
            }


            // ------------------------------------------------------
            // ATTENDANCE WINDOW VALIDATION
            // Shared by Create and Edit: each window must be
            // ordered, and the Time In window must end before the
            // Time Out window starts.
            // ------------------------------------------------------

            ValidateAttendanceWindows(
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
            // SAVE PROGRAM
            // ------------------------------------------------------

            _context.YouthPrograms.Add(
                program
            );

            await _context.SaveChangesAsync();


            // ------------------------------------------------------
            // AUTOMATIC BARANGAY ASSIGNMENT
            // ------------------------------------------------------

            await AutomaticallyAssignYouthToProgram(
                program.Id
            );


            TempData["Success"] =
                "Program created successfully. Matching registration requests, if any, are now waiting for approval.";


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
            YouthProgram program)
        {
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
            // SCHEDULE TEXT AUTO-FILL (PHASE 6)
            //
            // Only fills the free-text description when it is blank
            // AND structured date-times were provided. Existing
            // legacy schedule descriptions are preserved as-is.
            // ------------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    program.Schedule) &&
                (program.ScheduleStart.HasValue ||
                 program.ScheduleEnd.HasValue))
            {
                program.Schedule =
                    BuildScheduleText(
                        program.ScheduleStart,
                        program.ScheduleEnd
                    );


                ModelState.Remove(
                    nameof(program.Schedule)
                );
            }


            // ------------------------------------------------------
            // SCHEDULE DATE/TIME VALIDATION
            //
            // The end date/time must be after the start date/time
            // when both pickers were used.
            // ------------------------------------------------------

            if (program.ScheduleStart.HasValue &&
                program.ScheduleEnd.HasValue &&
                program.ScheduleEnd <= program.ScheduleStart)
            {
                ModelState.AddModelError(
                    nameof(program.ScheduleEnd),
                    "The schedule end date/time must be after the start date/time."
                );
            }


            // ------------------------------------------------------
            // ATTENDANCE WINDOW VALIDATION
            // Shared by Create and Edit: each window must be
            // ordered, and the Time In window must end before the
            // Time Out window starts.
            // ------------------------------------------------------

            ValidateAttendanceWindows(
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

            existingProgram.Status =
                string.IsNullOrWhiteSpace(
                    program.Status)
                    ? "Active"
                    : program.Status;

            existingProgram.Capacity =
                program.Capacity;

            existingProgram.Barangay =
                program.Barangay;


            await _context.SaveChangesAsync();


            // ------------------------------------------------------
            // AUTOMATICALLY ASSIGN NEW MATCHING YOUTH
            // ------------------------------------------------------

            await AutomaticallyAssignYouthToProgram(
                id
            );


            TempData["Success"] =
                "Program updated successfully. Matching registration requests, if any, are now waiting for approval.";


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
        // ==========================================================

        private async Task
            AutomaticallyAssignYouthToProgram(
                int programId)
        {
            var program =
                await _context.YouthPrograms
                    .FirstOrDefaultAsync(
                        p => p.Id == programId
                    );

            if (program == null)
            {
                return;
            }


            if (string.IsNullOrWhiteSpace(
                    program.Barangay))
            {
                return;
            }


            var programBarangay =
                NormalizeBarangayForComparison(
                    program.Barangay
                );


            if (string.IsNullOrWhiteSpace(
                    programBarangay))
            {
                return;
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
                            NormalizeBarangayForComparison(
                                y.Barangay
                            )
                            ==
                            programBarangay
                    )
                    .ToList();


            if (!matchingYouth.Any())
            {
                return;
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
                    continue;
                }


                // --------------------------------------------------
                // CAPACITY CHECK
                // --------------------------------------------------

                var approvedCount =
                    existingRegistrations
                        .Count(
                            r =>
                                r.Status ==
                                "Approved"
                        );


                if (approvedCount >=
                    program.Capacity)
                {
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


            await _context.SaveChangesAsync();


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
                catch (Exception)
                {
                    // A notification problem must not undo
                    // successful registration requests.
                }
            }
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


            await AutomaticallyAssignYouthToProgram(
                id
            );


            TempData["Success"] =
                "Matching youth were assigned to this program. New registrations are pending approval.";


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
        // DELETE PROGRAM
        // ==========================================================
        //
        // Admin only.
        //
        // Before deleting:
        //
        // Program
        //   ↓
        // Registrations
        //   ↓
        // Attendance
        //
        // Attendance records are removed first.
        // ==========================================================

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            Delete(
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
            // GET REGISTRATIONS
            // ------------------------------------------------------

            var registrations =
                await _context.ProgramRegistrations
                    .Where(
                        r =>
                            r.ProgramId ==
                            id
                    )
                    .ToListAsync();


            // ------------------------------------------------------
            // GET REGISTRATION IDS
            // ------------------------------------------------------

            var registrationIds =
                registrations
                    .Select(
                        r => r.Id
                    )
                    .ToList();


            // ------------------------------------------------------
            // GET ATTENDANCE
            // ------------------------------------------------------

            var attendance =
                await _context.AttendanceRecords
                    .Where(
                        a =>
                            registrationIds
                                .Contains(
                                    a.RegistrationId
                                )
                    )
                    .ToListAsync();


            // ------------------------------------------------------
            // PROTECTED HISTORICAL RECORDS (PHASE 2)
            //
            // Deleting a program cascades into registrations and
            // their attendance records, photo submissions, and
            // certificates. When any such history exists the hard
            // delete is refused and the admin is directed to the
            // inactive/completed workflow instead. Only programs
            // with no related records at all can still be deleted.
            // ------------------------------------------------------

            var photoCount =
                await _context.AttendancePhotoSubmissions
                    .CountAsync(
                        s =>
                            registrationIds
                                .Contains(
                                    s.RegistrationId
                                )
                    );


            var certificateCount =
                await _context.Certificates
                    .CountAsync(
                        c =>
                            registrationIds
                                .Contains(
                                    c.RegistrationId
                                )
                    );


            if (registrations.Any() ||
                attendance.Any() ||
                photoCount > 0 ||
                certificateCount > 0)
            {
                TempData["Error"] =
                    "Cannot delete: this program has registration, attendance, photo submission, or certificate history. " +
                    "Use 'Set Status' to mark the program Inactive or Completed so the history is preserved.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // ------------------------------------------------------
            // DELETE ATTENDANCE
            // ------------------------------------------------------

            if (attendance.Any())
            {
                _context.AttendanceRecords
                    .RemoveRange(
                        attendance
                    );
            }


            // ------------------------------------------------------
            // DELETE REGISTRATIONS
            // ------------------------------------------------------

            if (registrations.Any())
            {
                _context.ProgramRegistrations
                    .RemoveRange(
                        registrations
                    );
            }


            // ------------------------------------------------------
            // DELETE PROGRAM
            // ------------------------------------------------------

            _context.YouthPrograms
                .Remove(
                    program
                );


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Program deleted successfully.";


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
        // Each window must be ordered (start strictly before end)
        // and the Time In window must end at or before the Time
        // Out window starts, so the two windows can never
        // overlap.
        // ==========================================================

        private static void ValidateAttendanceWindows(
            YouthProgram program,
            ModelStateDictionary modelState)
        {
            if (program.TimeInStart >= program.TimeInEnd)
            {
                modelState.AddModelError(
                    nameof(program.TimeInEnd),
                    "The Time In window end must be after its start."
                );
            }


            if (program.TimeOutStart >= program.TimeOutEnd)
            {
                modelState.AddModelError(
                    nameof(program.TimeOutEnd),
                    "The Time Out window end must be after its start."
                );
            }


            if (program.TimeInEnd > program.TimeOutStart)
            {
                modelState.AddModelError(
                    nameof(program.TimeOutStart),
                    "The Time Out window must start after the Time In window ends."
                );
            }
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
                string? barangay)
        {
            if (string.IsNullOrWhiteSpace(
                    barangay))
            {
                return string.Empty;
            }


            return string.Join(
                " ",
                barangay
                    .Trim()
                    .Split(
                        ' ',
                        StringSplitOptions
                            .RemoveEmptyEntries
                    )
            );
        }


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
                string? barangay)
        {
            var value =
                NormalizeBarangayForStorage(
                    barangay
                );

            if (value.StartsWith(
                    "Barangay ",
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                value =
                    value.Substring(
                        "Barangay ".Length
                    ).Trim();
            }


            return value.ToLowerInvariant();
        }
    }
}
