using BYPManagementSystem.Data;
using BYPManagementSystem.Models;
using BYPManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BYPManagementSystem.Controllers
{
    [Authorize]
    public class YouthParticipantsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AttendancePhotoStorage _photoStorage;
        private readonly ILogger<YouthParticipantsController> _logger;

        public YouthParticipantsController(
            ApplicationDbContext context,
            AttendancePhotoStorage photoStorage,
            ILogger<YouthParticipantsController> logger)
        {
            _context = context;
            _photoStorage = photoStorage;
            _logger = logger;
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
        // DELETE YOUTH PARTICIPANT PERMANENTLY
        // ==========================================================
        //
        // Admin only, POST + antiforgery only.
        //
        // Removes an obsolete participant together with the
        // records that belong exclusively to that participant:
        //
        // Youth Participant
        //   ↑
        // Registrations (participant scoped)
        //   ↑
        // Attendance records    ─┐
        // Photo submissions     ─┤ by registration
        // Certificates          ─┘
        //
        // IDENTITY ACCOUNTS ARE PRESERVED. A YouthParticipant row
        // only stores a reference (UserId) to the ASP.NET Identity
        // user; deleting the participant never deletes the login
        // account, so the person can still sign in and unrelated
        // account data (roles, profile, notifications) is safe.
        //
        // Notification rows are account-scoped history in the
        // user's inbox and are preserved with the account.
        // Announcement logs are program-scoped and belong to the
        // program, not the participant.
        //
        // Everything runs in ONE transaction after locking the
        // participant row (SELECT ... FOR UPDATE), so concurrent
        // delete/edit requests serialize and a failure can never
        // leave a partially deleted participant. Stored photo
        // files are removed only AFTER the commit, only when no
        // surviving submission still references the file, and
        // only through AttendancePhotoStorage, which confines the
        // resolved path to the configured photo folder.
        // ==========================================================

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id)
        {
            // ------------------------------------------------------
            // RETRY ANY PHOTO FILE LEFT OVER FROM AN EARLIER DELETE
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


            var participant =
                default(YouthParticipant);

            var registrations =
                new List<ProgramRegistration>();

            var attendance =
                new List<AttendanceRecord>();

            var photos =
                new List<AttendancePhotoSubmission>();

            var certificates =
                new List<Certificate>();

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
                // or edit of the same participant waits for this
                // transaction and then re-reads the result.
                // --------------------------------------------------

                await _context.Database.ExecuteSqlRawAsync(
                    "SELECT Id FROM YouthParticipants WHERE Id = {0} FOR UPDATE",
                    id);


                participant =
                    await _context.YouthParticipants
                        .FirstOrDefaultAsync(
                            y => y.Id == id
                        );

                if (participant == null)
                {
                    // Already deleted by a concurrent request.
                    // Disposing the transaction rolls back.

                    return NotFound();
                }


                // --------------------------------------------------
                // LOAD THE PARTICIPANT'S REGISTRATIONS
                //
                // Includes legacy rows recorded before
                // YouthParticipantId was filled in, matched
                // through the linked Identity user. Registrations
                // that explicitly belong to a DIFFERENT
                // participant are never taken, even if some legacy
                // data anomaly links them to the same user.
                // --------------------------------------------------

                registrations =
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

                                &&

                                r.YouthParticipantId ==
                                    null
                            )
                        )
                        .ToListAsync();


                var registrationIds =
                    registrations
                        .Select(r => r.Id)
                        .ToList();


                photos =
                    await _context.AttendancePhotoSubmissions
                        .Where(s =>
                            registrationIds.Contains(
                                s.RegistrationId
                            )
                        )
                        .ToListAsync();


                certificates =
                    await _context.Certificates
                        .Where(c =>
                            registrationIds.Contains(
                                c.RegistrationId
                            )
                        )
                        .ToListAsync();


                attendance =
                    await _context.AttendanceRecords
                        .Where(a =>
                            registrationIds.Contains(
                                a.RegistrationId
                            )
                        )
                        .ToListAsync();


                // --------------------------------------------------
                // FILES OWNED EXCLUSIVELY BY THESE REGISTRATIONS
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
                            .Where(s =>
                                allFileNames.Contains(
                                    s.FileName
                                )

                                &&

                                !registrationIds.Contains(
                                    s.RegistrationId
                                )
                            )
                            .Select(s => s.FileName)
                            .Distinct()
                            .ToListAsync();

                exclusiveFileNames =
                    allFileNames
                        .Where(fn =>
                            !sharedFileNames.Contains(fn)
                        )
                        .ToList();


                // --------------------------------------------------
                // DELETE DEPENDENCIES BEFORE THE PARENT
                //
                // One SaveChanges inside the one transaction.
                // Registrations must go before the participant:
                // that foreign key is NO ACTION at the database.
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

                if (registrations.Count > 0)
                {
                    _context.ProgramRegistrations
                        .RemoveRange(registrations);
                }


                _context.YouthParticipants
                    .Remove(participant);


                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Youth participant {ParticipantId} delete hit a concurrency conflict; nothing was committed.",
                    id);

                TempData["Error"] =
                    "Could not delete: the participant changed while processing or was already deleted. " +
                    "Refresh the list and try again.";

                return RedirectToAction(
                    nameof(Index)
                );
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Youth participant {ParticipantId} delete failed at the database; transaction rolled back.",
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
                    "Youth participant {ParticipantId} delete failed; transaction rolled back.",
                    id);

                TempData["Error"] =
                    "Could not delete the youth participant due to a database error. " +
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
                        "Could not delete attendance photo file {FileName} after deleting participant {ParticipantId}. Queued for retry.",
                        fileName,
                        id);

                    _photoStorage.EnqueuePendingDelete(fileName);
                }
            }


            var message =
                $"Youth participant '{participant.FirstName} {participant.LastName}' permanently deleted together with " +
                $"{registrations.Count} registration(s), " +
                $"{attendance.Count} attendance record(s), " +
                $"{photos.Count} photo submission(s), and " +
                $"{certificates.Count} certificate(s). " +
                "The linked login account, if any, was preserved.";

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