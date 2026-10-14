using BYPManagementSystem.Data;
using BYPManagementSystem.Models;
using BYPManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BYPManagementSystem.Controllers
{
    [Authorize]
    public class YouthParticipantsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public YouthParticipantsController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // ==========================================================
        // PARTICIPANT LIST
        // ADMIN / STAFF
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> Index(
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

                    y.FirstName.Contains(search)

                    ||

                    y.LastName.Contains(search)

                    ||

                    (
                        y.ContactNumber != null &&
                        y.ContactNumber.Contains(search)
                    )

                    ||

                    (
                        y.Address != null &&
                        y.Address.Contains(search)
                    )

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
                    query =
                        query.Where(
                            y => y.IsActive
                        );
                }
                else if (status == "Inactive")
                {
                    query =
                        query.Where(
                            y => !y.IsActive
                        );
                }
            }


            var participants =
                await query
                    .OrderByDescending(
                        y => y.IsActive
                    )
                    .ThenBy(
                        y => y.LastName
                    )
                    .ThenBy(
                        y => y.FirstName
                    )
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

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> Details(
            int id)
        {
            var participant =
                await _context.YouthParticipants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        y => y.Id == id
                    );


            if (participant == null)
            {
                return NotFound();
            }


            return View(participant);
        }


        // ==========================================================
        // CREATE PARTICIPANT - GET
        // ADMIN / STAFF
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(
                new YouthParticipant
                {
                    IsActive = true
                }
            );
        }


        // ==========================================================
        // CREATE PARTICIPANT - POST
        // ADMIN / STAFF
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            YouthParticipant participant)
        {
            // ------------------------------------------------------
            // CLEAN TEXT
            // ------------------------------------------------------

            participant.FirstName =
                participant.FirstName?.Trim()
                ?? string.Empty;

            participant.LastName =
                participant.LastName?.Trim()
                ?? string.Empty;

            participant.Barangay =
                CleanBarangay(
                    participant.Barangay
                );

            participant.ContactNumber =
                string.IsNullOrWhiteSpace(
                    participant.ContactNumber)
                    ? null
                    : participant.ContactNumber.Trim();

            participant.Address =
                string.IsNullOrWhiteSpace(
                    participant.Address)
                    ? null
                    : participant.Address.Trim();


            // ------------------------------------------------------
            // REQUIRED BARANGAY
            // ------------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                participant.Barangay))
            {
                ModelState.AddModelError(
                    nameof(participant.Barangay),
                    "Barangay is required."
                );
            }


            if (!ModelState.IsValid)
            {
                return View(participant);
            }


            // ------------------------------------------------------
            // SET DEFAULT VALUES
            // ------------------------------------------------------

            participant.IsActive = true;

            participant.CreatedAt =
                DateTime.UtcNow;


            _context.YouthParticipants.Add(
                participant
            );


            await _context.SaveChangesAsync();


            // ------------------------------------------------------
            // AUTOMATIC PROGRAM ASSIGNMENT
            // SAME BARANGAY
            // ------------------------------------------------------

            await AutomaticallyAssignYouthToPrograms(
                participant.Id
            );


            TempData["Success"] =
                "Youth participant added successfully. Matching registration requests, if any, are now waiting for approval.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // EDIT PARTICIPANT - GET
        // ADMIN / STAFF
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> Edit(
            int id)
        {
            var participant =
                await _context.YouthParticipants
                    .FirstOrDefaultAsync(
                        y => y.Id == id
                    );


            if (participant == null)
            {
                return NotFound();
            }


            return View(participant);
        }


        // ==========================================================
        // EDIT PARTICIPANT - POST
        // ADMIN / STAFF
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            YouthParticipant model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // CLEAN TEXT
            // ------------------------------------------------------

            model.FirstName =
                model.FirstName?.Trim()
                ?? string.Empty;

            model.LastName =
                model.LastName?.Trim()
                ?? string.Empty;

            model.Barangay =
                CleanBarangay(
                    model.Barangay
                );

            model.ContactNumber =
                string.IsNullOrWhiteSpace(
                    model.ContactNumber)
                    ? null
                    : model.ContactNumber.Trim();

            model.Address =
                string.IsNullOrWhiteSpace(
                    model.Address)
                    ? null
                    : model.Address.Trim();


            // ------------------------------------------------------
            // REQUIRED BARANGAY
            // ------------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                model.Barangay))
            {
                ModelState.AddModelError(
                    nameof(model.Barangay),
                    "Barangay is required."
                );
            }


            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // ------------------------------------------------------
            // GET EXISTING PARTICIPANT
            // ------------------------------------------------------

            var participant =
                await _context.YouthParticipants
                    .FirstOrDefaultAsync(
                        y => y.Id == id
                    );


            if (participant == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // UPDATE ALLOWED FIELDS
            // ------------------------------------------------------

            participant.FirstName =
                model.FirstName;

            participant.LastName =
                model.LastName;

            participant.BirthDate =
                model.BirthDate;

            participant.ContactNumber =
                model.ContactNumber;

            participant.Address =
                model.Address;

            participant.Barangay =
                model.Barangay;


            await _context.SaveChangesAsync();


            // ------------------------------------------------------
            // AUTOMATIC PROGRAM ASSIGNMENT
            // ------------------------------------------------------

            await AutomaticallyAssignYouthToPrograms(
                participant.Id
            );


            TempData["Success"] =
                "Youth participant updated successfully. Matching programs were checked automatically.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // DEACTIVATE PARTICIPANT
        // ADMIN / STAFF
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(
            int id)
        {
            var participant =
                await _context.YouthParticipants
                    .FirstOrDefaultAsync(
                        y => y.Id == id
                    );


            if (participant == null)
            {
                return NotFound();
            }


            participant.IsActive =
                false;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Youth participant deactivated successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // ACTIVATE PARTICIPANT
        // ADMIN / STAFF
        // ==========================================================

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(
            int id)
        {
            var participant =
                await _context.YouthParticipants
                    .FirstOrDefaultAsync(
                        y => y.Id == id
                    );


            if (participant == null)
            {
                return NotFound();
            }


            participant.IsActive =
                true;


            await _context.SaveChangesAsync();


            // ------------------------------------------------------
            // RECHECK AUTOMATIC PROGRAM ASSIGNMENT
            // ------------------------------------------------------

            await AutomaticallyAssignYouthToPrograms(
                participant.Id
            );


            TempData["Success"] =
                "Youth participant activated successfully. Matching programs were checked automatically.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // DELETE PARTICIPANT
        // ADMIN ONLY
        // ==========================================================
        //
        // Delete order:
        //
        // Attendance
        //      ↓
        // Program Registrations
        //      ↓
        // Youth Participant
        //
        // This prevents orphaned dependent records.
        // ==========================================================

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id)
        {
            var participant =
                await _context.YouthParticipants
                    .FirstOrDefaultAsync(
                        y => y.Id == id
                    );


            if (participant == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // GET REGISTRATIONS
            //
            // Includes legacy rows recorded before YouthParticipantId
            // was filled in, matched through the linked Identity user.
            // ------------------------------------------------------

            var registrations =
                await _context.ProgramRegistrations
                    .Where(r =>
                        r.YouthParticipantId ==
                        id

                        ||

                        (
                            participant.UserId !=
                                null

                            &&

                            r.UserId ==
                                participant.UserId
                        )
                    )
                    .ToListAsync();


            var registrationIds =
                registrations
                    .Select(r => r.Id)
                    .ToList();


            // ------------------------------------------------------
            // GET ATTENDANCE
            // ------------------------------------------------------

            var attendance =
                await _context.AttendanceRecords
                    .Where(a =>
                        registrationIds.Contains(
                            a.RegistrationId
                        )
                    )
                    .ToListAsync();


            // ------------------------------------------------------
            // PROTECTED HISTORICAL RECORDS (PHASE 2)
            //
            // Registrations carry attendance records, photo
            // submissions, and certificates. When any history
            // exists the hard delete is refused and deactivation is
            // recommended instead. Only participants with no
            // registration or attendance history can be deleted.
            // ------------------------------------------------------

            if (registrations.Any() ||
                attendance.Any())
            {
                TempData["Error"] =
                    "Cannot delete: this participant has registration or attendance history. " +
                    "Use 'Deactivate' to preserve the history while preventing new activity.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // ------------------------------------------------------
            // DELETE DEPENDENCIES FIRST
            // ------------------------------------------------------

            _context.AttendanceRecords
                .RemoveRange(attendance);


            _context.ProgramRegistrations
                .RemoveRange(registrations);


            _context.YouthParticipants
                .Remove(participant);


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Youth participant deleted successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================================
        // AUTOMATICALLY ASSIGN YOUTH TO MATCHING PROGRAMS
        // ==========================================================
        //
        // Participant Barangay
        //         ==
        // Program Barangay
        //
        // Matching active programs create a PENDING
        // ProgramRegistration.
        //
        // The automatic match only creates the registration
        // REQUEST. The status stays "Pending" until an Admin or
        // authorized Staff member processes it through
        // Registration, so automatic matching never bypasses the
        // approval workflow. Only an "Approved" registration is
        // eligible for attendance recording.
        //
        // Attendance is NOT created automatically.
        // ==========================================================

        private async Task AutomaticallyAssignYouthToPrograms(
            int youthParticipantId)
        {
            var youth =
                await _context.YouthParticipants
                    .FirstOrDefaultAsync(
                        y => y.Id ==
                        youthParticipantId
                    );


            if (youth == null)
            {
                return;
            }


            if (!youth.IsActive)
            {
                return;
            }


            var youthBarangay =
                CleanBarangay(
                    youth.Barangay
                );


            if (string.IsNullOrWhiteSpace(
                youthBarangay))
            {
                return;
            }


            // ------------------------------------------------------
            // MATCH ACTIVE PROGRAMS
            // ------------------------------------------------------

            var programs =
                await _context.YouthPrograms
                    .Where(p =>
                        p.Status == "Active" &&
                        p.Barangay != null &&
                        p.Barangay.ToLower()
                            ==
                        youthBarangay.ToLower()
                    )
                    .ToListAsync();


            if (!programs.Any())
            {
                return;
            }


            // ------------------------------------------------------
            // EXISTING REGISTRATIONS
            //
            // Legacy rows recorded before YouthParticipantId was
            // filled in are matched through the linked Identity user
            // so the same registration is never created twice.
            // ------------------------------------------------------

            var existingRegistrations =
                await _context.ProgramRegistrations
                    .Where(r =>
                        r.YouthParticipantId ==
                        youthParticipantId

                        ||

                        (
                            youth.UserId !=
                                null

                            &&

                            r.UserId ==
                                youth.UserId
                        )
                    )
                    .ToListAsync();


            var createdRegistrations =
                new List<ProgramRegistration>();


            foreach (var program in programs)
            {
                // --------------------------------------------------
                // PREVENT DUPLICATE
                // --------------------------------------------------

                var existing =
                    existingRegistrations
                        .FirstOrDefault(r =>
                            r.ProgramId ==
                                program.Id
                        );


                if (existing != null)
                {
                    continue;
                }


                // --------------------------------------------------
                // CAPACITY
                // --------------------------------------------------

                var approvedCount =
                    await _context.ProgramRegistrations
                        .CountAsync(r =>
                            r.ProgramId ==
                                program.Id &&
                            r.Status ==
                                "Approved"
                        );


                if (approvedCount >=
                    program.Capacity)
                {
                    continue;
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
                    .Add(registration);


                existingRegistrations
                    .Add(registration);

                createdRegistrations
                    .Add(registration);
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
                        $"You have been automatically matched to a program in {youth.Barangay}. Your registration is waiting for approval.",
                        "Registration");


                if (autoNotification == null)
                {
                    continue;
                }


                _context.YouthNotifications
                    .Add(autoNotification);
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
        // BARANGAY CLEANER
        // ==========================================================

        private static string CleanBarangay(
            string? barangay)
        {
            if (string.IsNullOrWhiteSpace(
                barangay))
            {
                return string.Empty;
            }


            return barangay
                .Trim()
                .Replace(
                    "Barangay ",
                    "",
                    StringComparison.OrdinalIgnoreCase
                )
                .Trim();
        }
    }
}