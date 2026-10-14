using BYPManagementSystem.Data;
using BYPManagementSystem.Models;
using BYPManagementSystem.Services;
using BYPManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace BYPManagementSystem.Controllers
{
    [Authorize]
    public class RegistrationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public RegistrationController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // ==========================================================
        // REGISTRATION MANAGEMENT
        // ADMIN / STAFF
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? status)
        {
            var query =
                _context.ProgramRegistrations
                    .AsNoTracking()
                    .Include(r => r.Program)
                    .Include(r => r.YouthParticipant)
                    .AsQueryable();


            // ------------------------------------------------------
            // SEARCH
            // ------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                var searchTerm = search.ToLower();

                query = query.Where(r =>
                    (
                        r.YouthParticipant != null &&
                        (
                            (r.YouthParticipant.FirstName + " " +
                             r.YouthParticipant.LastName)
                            .ToLower().Contains(searchTerm)
                        )
                    )
                    ||
                    (
                        r.Program != null &&
                        r.Program.Name.ToLower().Contains(searchTerm)
                    )
                    ||
                    (
                        r.UserEmail != null &&
                        r.UserEmail.ToLower().Contains(searchTerm)
                    )
                    ||
                    (
                        r.Status.ToLower().Contains(searchTerm)
                    )
                );
            }


            // ------------------------------------------------------
            // STATUS FILTER
            // ------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(status) &&
                status != "All")
            {
                query = query.Where(r =>
                    r.Status == status);
            }


            var registrations =
                await query
                    .OrderByDescending(r => r.RegisteredAt)
                    .ToListAsync();


            // ------------------------------------------------------
            // SUMMARY COUNTS
            // ------------------------------------------------------

            var allRegistrations =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .ToListAsync();


            ViewBag.TotalRegistrations =
                allRegistrations.Count;

            ViewBag.PendingRegistrations =
                allRegistrations.Count(
                    r => r.Status == "Pending"
                );

            ViewBag.ApprovedRegistrations =
                allRegistrations.Count(
                    r => r.Status == "Approved"
                );

            ViewBag.RejectedRegistrations =
                allRegistrations.Count(
                    r => r.Status == "Rejected"
                );


            ViewBag.Search =
                search;

            ViewBag.Status =
                status;


            return View(registrations);
        }


        // ==========================================================
        // PARTICIPANT DIRECTORY
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> Participants(
            string? search,
            string? status)
        {
            var query =
                _context.YouthParticipants
                    .AsNoTracking()
                    .AsQueryable();


            // ------------------------------------------------------
            // SEARCH
            // ------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(y =>
                    (
                        y.FirstName + " " +
                        y.LastName
                    ).Contains(search)
                    ||
                    (y.ContactNumber != null &&
                     y.ContactNumber.Contains(search))
                    ||
                    (y.Address != null &&
                     y.Address.Contains(search))
                    ||
                    y.Barangay.Contains(search)
                );
            }


            // ------------------------------------------------------
            // STATUS FILTER
            // ------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(status) &&
                status != "All")
            {
                if (status == "Active")
                {
                    query = query.Where(
                        y => y.IsActive
                    );
                }
                else if (status == "Inactive")
                {
                    query = query.Where(
                        y => !y.IsActive
                    );
                }
            }


            var participants =
                await query
                    .OrderByDescending(y => y.IsActive)
                    .ThenBy(y => y.LastName)
                    .ThenBy(y => y.FirstName)
                    .ToListAsync();


            ViewBag.Search =
                search;

            ViewBag.Status =
                status;


            return View(participants);
        }


        // ==========================================================
        // PARTICIPANT DETAILS
        // ==========================================================
        //
        // Shows:
        // - Participant information
        // - Account
        // - Assigned programs
        // - Registration statuses
        //
        // This is the page used in:
        //
        // REGISTRATION
        //      ↓
        // YOUTH PARTICIPANTS
        //      ↓
        // PARTICIPANT
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> Participant(
            int id)
        {
            var youth =
                await _context.YouthParticipants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        y => y.Id == id
                    );


            if (youth == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // GET USER ACCOUNT EMAIL
            // ------------------------------------------------------

            string? userEmail =
                null;


            if (!string.IsNullOrWhiteSpace(
                youth.UserId))
            {
                var user =
                    await _userManager.FindByIdAsync(
                        youth.UserId
                    );

                userEmail =
                    user?.Email;
            }


            // ------------------------------------------------------
            // GET REGISTRATIONS
            // ------------------------------------------------------

            var registrations =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .Include(r => r.Program)
                    .Include(r => r.YouthParticipant)
                    .Where(r =>
                        r.YouthParticipantId == id
                    )
                    .OrderByDescending(
                        r => r.RegisteredAt
                    )
                    .ToListAsync();


            // ------------------------------------------------------
            // CREATE VIEW MODEL
            // ------------------------------------------------------

            var model =
                new RegistrationParticipantViewModel
                {
                    YouthParticipant =
                        youth,

                    UserEmail =
                        userEmail,

                    HasUserAccount =
                        !string.IsNullOrWhiteSpace(
                            youth.UserId
                        ),

                    Registrations =
                        registrations
                };


            return View(model);
        }


        // ==========================================================
        // REGISTER TO PROGRAM - GET
        // USER / YOUTH
        // ==========================================================

        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> Register(
            int programId)
        {
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


            // Only active programs can receive
            // public registration requests.

            if (program.Status != "Active")
            {
                TempData["Error"] =
                    "This program is not currently available for registration.";

                return RedirectToAction(
                    "Index",
                    "Programs"
                );
            }


            // ------------------------------------------------------
            // CHECK CAPACITY
            // ------------------------------------------------------

            var approvedCount =
                await _context.ProgramRegistrations
                    .CountAsync(r =>
                        r.ProgramId == programId &&
                        r.Status == "Approved"
                    );


            if (approvedCount >= program.Capacity)
            {
                TempData["Error"] =
                    "This program is already full.";

                return RedirectToAction(
                    "Index",
                    "Programs"
                );
            }


            ViewBag.Program =
                program;


            return View(
                new ProgramRegistration
                {
                    ProgramId =
                        program.Id,

                    Status =
                        "Pending"
                }
            );
        }


        // ==========================================================
        // REGISTER TO PROGRAM - POST
        // USER / YOUTH
        // ==========================================================

        [Authorize(Roles = "User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            int programId,
            ProgramRegistration model)
        {
            // ------------------------------------------------------
            // GET CURRENT USER
            // ------------------------------------------------------

            var userId =
                _userManager.GetUserId(User);

            var userEmail =
                User.Identity?.Name;


            if (string.IsNullOrWhiteSpace(
                userId))
            {
                return Unauthorized();
            }


            // ------------------------------------------------------
            // GET PROGRAM
            // ------------------------------------------------------

            var program =
                await _context.YouthPrograms
                    .FirstOrDefaultAsync(
                        p => p.Id == programId
                    );


            if (program == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // PROGRAM MUST BE ACTIVE
            // ------------------------------------------------------

            if (program.Status != "Active")
            {
                TempData["Error"] =
                    "This program is not currently available for registration.";

                return RedirectToAction(
                    "Index",
                    "Programs"
                );
            }


            // ------------------------------------------------------
            // FIND YOUTH PARTICIPANT
            // ------------------------------------------------------

            var youth =
                await _context.YouthParticipants
                    .FirstOrDefaultAsync(
                        y => y.UserId == userId
                    );


            // ------------------------------------------------------
            // ACCOUNT MAY NOT YET HAVE A PARTICIPANT RECORD
            // Create one automatically so the registration can
            // be submitted successfully.
            // ------------------------------------------------------

            if (youth == null)
            {
                var namePart =
                    (userEmail ?? "youth")
                        .Split('@')[0];

                if (string.IsNullOrWhiteSpace(namePart))
                {
                    namePart = "Youth";
                }

                youth =
                    new YouthParticipant
                    {
                        UserId =
                            userId,

                        IsActive =
                            true,

                        FirstName =
                            namePart.Trim(),

                        LastName =
                            "User",

                        Barangay =
                            "Not Specified",

                        CreatedAt =
                            DateTime.UtcNow
                    };

                _context.YouthParticipants.Add(youth);

                await _context.SaveChangesAsync();
            }


            if (!youth.IsActive)
            {
                TempData["Error"] =
                    "Your youth participant record is inactive.";

                return RedirectToAction(
                    "MyRegistrations"
                );
            }


            // ------------------------------------------------------
            // PREVENT DUPLICATE REGISTRATION
            // ------------------------------------------------------

            var existingRegistration =
                await _context.ProgramRegistrations
                    .FirstOrDefaultAsync(r =>
                        r.ProgramId == programId &&
                        (
                            r.YouthParticipantId ==
                                youth.Id
                            ||
                            r.UserId == userId
                        ));


            if (existingRegistration != null)
            {
                TempData["Error"] =
                    "You are already registered for this program.";

                return RedirectToAction(
                    "MyRegistrations"
                );
            }


            // ------------------------------------------------------
            // CAPACITY
            // ------------------------------------------------------

            var approvedCount =
                await _context.ProgramRegistrations
                    .CountAsync(r =>
                        r.ProgramId == programId &&
                        r.Status == "Approved"
                    );


            if (approvedCount >= program.Capacity)
            {
                TempData["Error"] =
                    "This program is already full.";

                return RedirectToAction(
                    "Index",
                    "Programs"
                );
            }


            // ------------------------------------------------------
            // CREATE REGISTRATION
            // ------------------------------------------------------

            var registration =
                new ProgramRegistration
                {
                    ProgramId =
                        program.Id,

                    YouthParticipantId =
                        youth.Id,

                    UserId =
                        userId,

                    UserEmail =
                        userEmail,

                    Status =
                        "Pending",

                    RegisteredAt =
                        DateTime.UtcNow
                };


            _context.ProgramRegistrations
                .Add(registration);


            await _context.SaveChangesAsync();


            // ------------------------------------------------------
            // NOTIFICATION - REGISTRATION SUBMITTED
            // ------------------------------------------------------

            var submittedNotification =
                NotificationHelper.ForRegistration(
                    registration,
                    "Registration Submitted",
                    $"Your registration for \"{program.Name}\" has been submitted and is waiting for approval.",
                    "Registration");


            if (submittedNotification != null)
            {
                _context.YouthNotifications
                    .Add(submittedNotification);


                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception)
                {
                    // A notification problem must not
                    // undo a successful registration.
                }
            }


            TempData["Success"] =
                "Registration submitted successfully and is waiting for approval.";


            return RedirectToAction(
                nameof(MyRegistrations)
            );
        }


        // ==========================================================
        // MY REGISTRATIONS
        // USER / YOUTH
        // ==========================================================

        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> MyRegistrations()
        {
            var userId =
                _userManager.GetUserId(User);


            if (string.IsNullOrWhiteSpace(
                userId))
            {
                return Unauthorized();
            }


            var userEmail =
                User.Identity?.Name;

            var registrations =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .Include(r => r.Program)
                    .Include(r => r.YouthParticipant)
                    .Where(r =>
                        r.UserId == userId
                        ||
                        (
                            r.YouthParticipant != null &&
                            r.YouthParticipant.UserId == userId
                        )
                        ||
                        (
                            !string.IsNullOrWhiteSpace(userEmail) &&
                            r.UserEmail == userEmail
                        )
                    )
                    .OrderByDescending(
                        r => r.RegisteredAt
                    )
                    .ToListAsync();


            return View(registrations);
        }


        // ==========================================================
        // UPDATE REGISTRATION STATUS
        // ADMIN / STAFF
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int id,
            string status)
        {
            // ------------------------------------------------------
            // ONLY VALID STATUSES
            // ------------------------------------------------------

            var validStatuses =
                new[]
                {
                    "Pending",
                    "Approved",
                    "Rejected"
                };


            if (!validStatuses.Contains(
                status))
            {
                TempData["Error"] =
                    "Invalid registration status.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // ------------------------------------------------------
            // FIND REGISTRATION
            // ------------------------------------------------------

            var registration =
                await _context.ProgramRegistrations
                    .Include(r => r.YouthParticipant)
                    .FirstOrDefaultAsync(
                        r => r.Id == id
                    );


            if (registration == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // TRANSACTIONAL STATUS UPDATE (PHASE 4)
            //
            // Approvals for the same program are serialized with a
            // row lock on the program row, so two simultaneous
            // approval requests can never both pass the capacity
            // check. The capacity is recounted inside the locked
            // transaction, and the status change only persists when
            // the whole transaction commits successfully. A failed
            // approval rolls back and leaves the registration
            // unchanged.
            // ------------------------------------------------------

            string previousStatus;


            await using (var transaction =
                await _context.Database.BeginTransactionAsync())
            {
                // --------------------------------------------------
                // SERIALIZE APPROVALS PER PROGRAM
                // --------------------------------------------------

                await _context.Database.ExecuteSqlRawAsync(
                    "SELECT Id FROM YouthPrograms WHERE Id = {0} FOR UPDATE",
                    registration.ProgramId);


                // --------------------------------------------------
                // RE-READ AFTER LOCKING
                //
                // Captures any concurrent status change that
                // committed before this request acquired the lock.
                // --------------------------------------------------

                await _context.Entry(registration)
                    .ReloadAsync();


                previousStatus =
                    registration.Status;


                // --------------------------------------------------
                // APPROVAL CAPACITY CHECK (INSIDE THE LOCK)
                // --------------------------------------------------

                if (status == "Approved" &&
                    registration.Status != "Approved")
                {
                    var approvedCount =
                        await _context.ProgramRegistrations
                            .CountAsync(r =>
                                r.ProgramId ==
                                    registration.ProgramId &&
                                r.Status ==
                                    "Approved"
                            );


                    var program =
                        await _context.YouthPrograms
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                p =>
                                    p.Id ==
                                    registration.ProgramId
                            );


                    if (program == null)
                    {
                        await transaction.RollbackAsync();

                        return NotFound();
                    }


                    if (approvedCount >=
                        program.Capacity)
                    {
                        await transaction.RollbackAsync();

                        TempData["Error"] =
                            "The program has reached its capacity.";

                        return RedirectToAction(
                            nameof(Index)
                        );
                    }
                }


                // --------------------------------------------------
                // UPDATE STATUS
                // --------------------------------------------------

                registration.Status =
                    status;


                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }


            // ------------------------------------------------------
            // NOTIFICATION - STATUS CHANGED
            // Only when a real status change occurred.
            // ------------------------------------------------------

            if (previousStatus != status)
            {
                var statusProgramName =
                    await _context.YouthPrograms
                        .AsNoTracking()
                        .Where(p => p.Id ==
                            registration.ProgramId)
                        .Select(p => p.Name)
                        .FirstOrDefaultAsync()
                    ?? "the program";


                string statusTitle;
                string statusMessage;
                string statusType;


                if (status == "Approved")
                {
                    statusTitle = "Registration Approved";
                    statusMessage = $"Your registration for \"{statusProgramName}\" has been approved.";
                    statusType = "Approval";
                }
                else if (status == "Rejected")
                {
                    statusTitle = "Registration Rejected";
                    statusMessage = $"Your registration for \"{statusProgramName}\" has been rejected.";
                    statusType = "Rejection";
                }
                else
                {
                    statusTitle = "Registration Updated";
                    statusMessage = $"Your registration for \"{statusProgramName}\" is now marked as {status}.";
                    statusType = "Registration";
                }


                var statusNotification =
                    NotificationHelper.ForRegistration(
                        registration,
                        statusTitle,
                        statusMessage,
                        statusType);


                if (statusNotification != null)
                {
                    _context.YouthNotifications
                        .Add(statusNotification);


                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception)
                    {
                        // A notification problem must not
                        // fail the staff status update.
                    }
                }
            }


            TempData["Success"] =
                $"Registration status changed to {status}.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // ATTENDANCE FROM REGISTRATION
        // ADMIN / STAFF
        // ==========================================================
        //
        // Only Approved registrations can proceed to attendance.
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> Attendance(
            int id)
        {
            var registration =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .Include(r => r.Program)
                    .Include(r => r.YouthParticipant)
                    .FirstOrDefaultAsync(
                        r => r.Id == id
                    );


            if (registration == null)
            {
                return NotFound();
            }


            if (registration.Status != "Approved")
            {
                TempData["Error"] =
                    "Attendance is available only for approved registrations.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            if (!registration.YouthParticipantId
                .HasValue)
            {
                TempData["Error"] =
                    "This registration is not linked to a youth participant.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            return RedirectToAction(
                "Records",
                "Attendance",
                new
                {
                    programId =
                        registration.ProgramId,

                    youthParticipantId =
                        registration.YouthParticipantId.Value
                }
            );
        }
    }
}
