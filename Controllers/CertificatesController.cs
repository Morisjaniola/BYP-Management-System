using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;
using BYPManagementSystem.Data;
using BYPManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BYPManagementSystem.Controllers
{
    // ==========================================================
    // CERTIFICATES CONTROLLER
    // 
    // Manages issuance and viewing of completion certificates.
    // Admin-only for issuance; Youth can view/print their own.
    // ==========================================================

    [Authorize]
    public class CertificatesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CertificatesController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // ==========================================================
        // ADMIN - CERTIFICATES DASHBOARD
        // 
        // Lists completed programs and their eligible participants.
        // Shows attendance summary and certificate issuance status.
        // ==========================================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index(
            int? programId,
            string? filter)
        {
            // ------------------------------------------------------
            // LOAD COMPLETED PROGRAMS
            // ------------------------------------------------------

            var completedPrograms =
                await _context.YouthPrograms
                    .AsNoTracking()
                    .Where(
                        p => p.Status == "Completed"
                    )
                    .OrderBy(
                        p => p.Name
                    )
                    .ToListAsync();


            // ------------------------------------------------------
            // SELECTED PROGRAM
            // ------------------------------------------------------

            YouthProgram? selectedProgram = null;

            if (programId.HasValue)
            {
                selectedProgram =
                    completedPrograms.FirstOrDefault(
                        p => p.Id == programId.Value
                    );
            }

            ViewBag.Programs = completedPrograms;
            ViewBag.SelectedProgramId =
                selectedProgram?.Id;
            ViewBag.Filter = filter ?? "All";


            // ------------------------------------------------------
            // IF NO PROGRAM SELECTED, SHOW EMPTY STATE
            // ------------------------------------------------------

            if (selectedProgram == null)
            {
                return View(
                    new List<CertificateEligibilityViewModel>()
                );
            }


            // ------------------------------------------------------
            // LOAD APPROVED REGISTRATIONS FOR SELECTED PROGRAM
            // ------------------------------------------------------

            var registrations =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .Include(
                        r => r.YouthParticipant
                    )
                    .Where(
                        r =>
                            r.ProgramId ==
                            selectedProgram.Id &&
                            r.Status == "Approved"
                    )
                    .OrderBy(
                        r =>
                            r.YouthParticipant !=
                            null
                                ? r.YouthParticipant
                                    .LastName
                                : ""
                    )
                    .ThenBy(
                        r =>
                            r.YouthParticipant !=
                            null
                                ? r.YouthParticipant
                                    .FirstName
                                : ""
                    )
                    .ToListAsync();


            // ------------------------------------------------------
            // LOAD EXISTING CERTIFICATES FOR THIS PROGRAM
            // ------------------------------------------------------

            var registrationIds =
                registrations.Select(
                    r => r.Id
                ).ToList();

            var existingCertificates =
                await _context.Certificates
                    .AsNoTracking()
                    .Where(
                        c =>
                            registrationIds
                                .Contains(
                                    c.RegistrationId
                                )
                    )
                    .ToDictionaryAsync(
                        c => c.RegistrationId
                    );


            // ------------------------------------------------------
            // LOAD ATTENDANCE RECORDS FOR THESE REGISTRATIONS
            // 
            // Summary: present count + total records per registration.
            // ======================================================

            var attendanceSummary =
                await _context.AttendanceRecords
                    .AsNoTracking()
                    .Where(
                        a =>
                            registrationIds
                                .Contains(
                                    a.RegistrationId
                                )
                    )
                    .GroupBy(
                        a => a.RegistrationId
                    )
                    .Select(
                        g => new
                        {
                            RegistrationId =
                                g.Key,

                            PresentCount =
                                g.Count(
                                    a =>
                                        a.Status ==
                                        "Present"
                                ),

                            TotalRecords =
                                g.Count()
                        }
                    )
                    .ToDictionaryAsync(
                        x =>
                            x.RegistrationId
                    );


            // ------------------------------------------------------
            // BUILD VIEW MODEL
            // ------------------------------------------------------

            var viewModel =
                new List<
                    CertificateEligibilityViewModel
                >();

            foreach (var reg in registrations)
            {
                var participant =
                    reg.YouthParticipant;

                var fullName =
                    participant != null
                        ? $"{participant.FirstName} {participant.LastName}"
                        : reg.UserEmail ??
                          "Unknown Participant";

                var att =
                    attendanceSummary.TryGetValue(
                        reg.Id,
                        out var summary
                    )
                        ? summary
                        : null;

                var hasCert =
                    existingCertificates
                        .ContainsKey(
                            reg.Id
                        );

                var cert =
                    hasCert
                        ? existingCertificates[
                            reg.Id]
                        : null;

                var model =
                    new CertificateEligibilityViewModel
                    {
                        RegistrationId =
                            reg.Id,

                        ParticipantName =
                            fullName,

                        ParticipantEmail =
                            reg.UserEmail,

                        ProgramId =
                            selectedProgram.Id,

                        ProgramName =
                            selectedProgram.Name,

                        PresentCount =
                            att?.PresentCount ?? 0,

                        TotalAttendanceRecords =
                            att?.TotalRecords ?? 0,

                        HasCertificate =
                            hasCert,

                        CertificateNumber =
                            cert?.CertificateNumber,

                        CertificateIssuedAt =
                            cert?.IssuedAt
                    };

                viewModel.Add(model);
            }


            // ------------------------------------------------------
            // APPLY FILTER
            // 
            // All: show everyone
            // Eligible: show those without certificates
            // Issued: show those with certificates
            // ======================================================

            if (filter == "Eligible")
            {
                viewModel =
                    viewModel
                        .Where(
                            m =>
                                !m.HasCertificate
                        )
                        .ToList();
            }
            else if (filter == "Issued")
            {
                viewModel =
                    viewModel
                        .Where(
                            m =>
                                m.HasCertificate
                        )
                        .ToList();
            }


            return View(viewModel);
        }


        // ==========================================================
        // ADMIN - ISSUE CERTIFICATE (GET)
        // 
        // Shows confirmation page with participant details and
        // attestation checkbox. The admin must explicitly confirm
        // the participant has completed the program.
        // ==========================================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Issue(
            int registrationId)
        {
            // ------------------------------------------------------
            // LOAD REGISTRATION WITH PROGRAM AND PARTICIPANT
            // ------------------------------------------------------

            var registration =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .Include(
                        r => r.Program
                    )
                    .Include(
                        r => r.YouthParticipant
                    )
                    .FirstOrDefaultAsync(
                        r =>
                            r.Id ==
                            registrationId
                    );

            if (registration == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // VALIDATE: PROGRAM MUST BE COMPLETED
            // ------------------------------------------------------

            if (
                registration.Program == null ||
                registration.Program.Status !=
                    "Completed"
            )
            {
                TempData["Error"] =
                    "Program must be marked as Completed before issuing certificates.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        programId =
                            registration.ProgramId
                    }
                );
            }


            // ------------------------------------------------------
            // VALIDATE: REGISTRATION MUST BE APPROVED
            // ------------------------------------------------------

            if (registration.Status != "Approved")
            {
                TempData["Error"] =
                    "Registration must be Approved before issuing a certificate.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        programId =
                            registration.ProgramId
                    }
                );
            }


            // ------------------------------------------------------
            // CHECK FOR EXISTING CERTIFICATE
            // ------------------------------------------------------

            var existingCert =
                await _context.Certificates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        c =>
                            c.RegistrationId ==
                            registration.Id
                    );

            if (existingCert != null)
            {
                TempData["Error"] =
                    $"Certificate already issued: {existingCert.CertificateNumber}";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = existingCert.Id
                    }
                );
            }


            // ------------------------------------------------------
            // BUILD VIEW MODEL
            // ------------------------------------------------------

            var participant =
                registration.YouthParticipant;

            var fullName =
                participant != null
                    ? $"{participant.FirstName} {participant.LastName}"
                    : registration.UserEmail ??
                      "Unknown Participant";

            var program =
                registration.Program;

            var viewModel =
                new IssueCertificateViewModel
                {
                    RegistrationId =
                        registration.Id,

                    ParticipantName =
                        fullName,

                    ParticipantEmail =
                        registration.UserEmail,

                    ProgramId =
                        program.Id,

                    ProgramName =
                        program.Name,

                    ProgramScheduleEnd =
                        program.ScheduleEnd,

                    // Completion date = ScheduleEnd if set, else null (admin must handle)
                    CompletionDate =
                        program.ScheduleEnd?.Date
                };

            return View(viewModel);
        }


        // ==========================================================
        // ADMIN - ISSUE CERTIFICATE (POST)
        // 
        // Validates and creates the certificate. Requires explicit
        // attestation from the admin (no completion criterion exists
        // in the system, so this is the authorized review step).
        // ==========================================================

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Issue(
            IssueCertificateViewModel model)
        {
            // ------------------------------------------------------
            // VALIDATE ATTESTATION
            // 
            // The system has no automatic completion rule, so the
            // admin must explicitly confirm completion.
            // ======================================================

            if (!model.ConfirmCompletion)
            {
                ModelState.AddModelError(
                    nameof(
                        model.ConfirmCompletion
                    ),
                    "You must confirm that this participant has completed the program."
                );
            }


            // ------------------------------------------------------
            // VALIDATE COMPLETION DATE
            // 
            // Cannot fabricate a completion date. If the program
            // has no ScheduleEnd, the admin must enter one.
            // ======================================================

            if (
                model.CompletionDate ==
                default(DateTime)
            )
            {
                ModelState.AddModelError(
                    nameof(
                        model.CompletionDate
                    ),
                    "Completion date is required. If the program has no scheduled end date, enter the date you are confirming completion."
                );
            }


            if (!ModelState.IsValid)
            {
                // Reload program data for redisplay
                var reg =
                    await _context
                        .ProgramRegistrations
                        .AsNoTracking()
                        .Include(
                            r => r.Program
                        )
                        .Include(
                            r =>
                                r.YouthParticipant
                        )
                        .FirstOrDefaultAsync(
                            r =>
                                r.Id ==
                                model.RegistrationId
                        );

                if (reg != null)
                {
                    var participant =
                        reg.YouthParticipant;

                    model.ParticipantName =
                        participant != null
                            ? $"{participant.FirstName} {participant.LastName}"
                            : reg.UserEmail ??
                              "Unknown";

                    model.ProgramName =
                        reg.Program?.Name ??
                        "Unknown";

                    model.ProgramScheduleEnd =
                        reg.Program?.ScheduleEnd;

                    if (
                        model.CompletionDate ==
                        default(DateTime)
                    )
                    {
                        model.CompletionDate =
                            reg.Program
                                ?.ScheduleEnd
                                ?.Date;
                    }
                }

                return View(model);
            }


            // ------------------------------------------------------
            // RE-VALIDATE REGISTRATION AND PROGRAM STATUS
            // 
            // Server-side checks (never trust client).
            // ======================================================

            var registration =
                await _context.ProgramRegistrations
                    .Include(
                        r => r.Program
                    )
                    .Include(
                        r => r.YouthParticipant
                    )
                    .FirstOrDefaultAsync(
                        r =>
                            r.Id ==
                            model.RegistrationId
                    );

            if (registration == null)
            {
                return NotFound();
            }

            if (
                registration.Program == null ||
                registration.Program.Status !=
                    "Completed"
            )
            {
                TempData["Error"] =
                    "Program must be marked as Completed.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        programId =
                            registration.ProgramId
                    }
                );
            }

            if (registration.Status != "Approved")
            {
                TempData["Error"] =
                    "Registration must be Approved.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        programId =
                            registration.ProgramId
                    }
                );
            }


            // ------------------------------------------------------
            // CHECK FOR DUPLICATE (RACE CONDITION)
            // ------------------------------------------------------

            var duplicateExists =
                await _context.Certificates
                    .AnyAsync(
                        c =>
                            c.RegistrationId ==
                            registration.Id
                    );

            if (duplicateExists)
            {
                TempData["Error"] =
                    "A certificate already exists for this registration.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        programId =
                            registration.ProgramId
                    }
                );
            }


            // ------------------------------------------------------
            // GET CURRENT ADMIN USER
            // ------------------------------------------------------

            var userId =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;

            var userName =
                User.Identity?.Name ?? "Admin";


            // ------------------------------------------------------
            // GENERATE UNIQUE CERTIFICATE NUMBER
            // 
            // Format: BYP-{year}-{8 alphanumeric chars}
            // Retry on collision (extremely unlikely).
            // ======================================================

            string certNumber;
            int attempts = 0;
            const int maxAttempts = 10;

            do
            {
                certNumber =
                    GenerateCertificateNumber();

                attempts++;

                if (attempts > maxAttempts)
                {
                    TempData["Error"] =
                        "Failed to generate a unique certificate number. Please try again.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            programId =
                                registration.ProgramId
                        }
                    );
                }
            } while (
                await _context.Certificates
                    .AnyAsync(
                        c =>
                            c.CertificateNumber ==
                            certNumber
                    )
            );


            // ------------------------------------------------------
            // CREATE CERTIFICATE
            // ------------------------------------------------------

            var participant2 =
                registration.YouthParticipant;

            var fullName =
                participant2 != null
                    ? $"{participant2.FirstName} {participant2.LastName}"
                    : registration.UserEmail ??
                      "Unknown Participant";

            var certificate = new Certificate
            {
                CertificateNumber =
                    certNumber,

                RegistrationId =
                    registration.Id,

                ParticipantName =
                    fullName,

                ProgramName =
                    registration.Program.Name,

                CompletionDate =
                    model.CompletionDate!
                        .Value
                        .Date,

                IssuedAt =
                    DateTime.UtcNow,

                IssuedByUserId =
                    userId,

                IssuedByUserName =
                    userName
            };

            _context.Certificates.Add(
                certificate
            );

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // ------------------------------------------------------
                // RACE CONDITION: another request created a certificate
                // for this registration between our pre-check and the
                // insert. The unique index on RegistrationId rejected
                // the duplicate. Remove the pending entity so the
                // context is not left in a broken state.
                // ======================================================

                _context.Entry(
                    certificate
                ).State =
                    EntityState.Detached;

                TempData["Error"] =
                    "A certificate was already issued for this registration.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        programId =
                            registration.ProgramId
                    }
                );
            }


            // ------------------------------------------------------
            // SUCCESS
            // ------------------------------------------------------

            TempData["Success"] =
                $"Certificate {certNumber} issued successfully.";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = certificate.Id
                }
            );
        }


        // ==========================================================
        // ADMIN / OWNER - CERTIFICATE DETAILS
        // 
        // Shows certificate preview with print/download options.
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Details(
            int id)
        {
            var certificate =
                await _context.Certificates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        c =>
                            c.Id == id
                    );

            if (certificate == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // AUTHORIZATION CHECK
            // 
            // Admin: can view any certificate.
            // User (Youth): can only view own certificates.
            // ======================================================

            var isAdmin =
                User.IsInRole("Admin");

            if (!isAdmin)
            {
                var userId =
                    User.FindFirst(
                        ClaimTypes.NameIdentifier
                    )?.Value;

                var userEmail =
                    User.Identity?.Name;

                if (string.IsNullOrWhiteSpace(
                    userId
                ))
                {
                    return Unauthorized();
                }


                // ------------------------------------------------------
                // LOAD REGISTRATION TO VERIFY OWNERSHIP
                // ------------------------------------------------------

                var registration =
                    await _context
                        .ProgramRegistrations
                        .AsNoTracking()
                        .Include(
                            r =>
                                r.YouthParticipant
                        )
                        .FirstOrDefaultAsync(
                            r =>
                                r.Id ==
                                certificate.RegistrationId
                        );

                if (registration == null)
                {
                    return NotFound();
                }


                // ------------------------------------------------------
                // CHECK OWNERSHIP VIA USER ID, EMAIL, OR LINKED
                // YOUTH PARTICIPANT (LEGACY ROWS WHERE THE
                // REGISTRATION WAS STORED BEFORE USERID WAS FILLED IN)
                // ======================================================

                var ownsRegistration =
                    registration.UserId ==
                        userId ||
                    (
                        !string.IsNullOrWhiteSpace(
                            userEmail
                        ) &&
                        registration.UserEmail ==
                            userEmail
                    ) ||
                    (
                        registration.YouthParticipant !=
                            null

                        &&

                        registration.YouthParticipant.UserId
                            ==
                            userId
                    );

                if (!ownsRegistration)
                {
                    return Forbid();
                }
            }


            return View(certificate);
        }


        // ==========================================================
        // PRINT - STANDALONE PRINT-READY VIEW
        // 
        // Serves the certificate as a standalone HTML page optimized
        // for printing. Use @media print CSS to hide UI chrome.
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Print(
            int id)
        {
            var certificate =
                await _context.Certificates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        c =>
                            c.Id == id
                    );

            if (certificate == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // AUTHORIZATION (same as Details)
            // ------------------------------------------------------

            var isAdmin =
                User.IsInRole("Admin");

            if (!isAdmin)
            {
                var userId =
                    User.FindFirst(
                        ClaimTypes.NameIdentifier
                    )?.Value;

                var userEmail =
                    User.Identity?.Name;

                if (string.IsNullOrWhiteSpace(
                    userId
                ))
                {
                    return Unauthorized();
                }

                var registration =
                    await _context
                        .ProgramRegistrations
                        .AsNoTracking()
                        .Include(
                            r =>
                                r.YouthParticipant
                        )
                        .FirstOrDefaultAsync(
                            r =>
                                r.Id ==
                                certificate.RegistrationId
                        );

                if (registration == null)
                {
                    return NotFound();
                }

                // Ownership via UserId, email, or the linked
                // youth participant record (legacy rows where
                // the registration was stored before UserId
                // was filled in).
                var ownsRegistration =
                    registration.UserId ==
                        userId ||
                    (
                        !string.IsNullOrWhiteSpace(
                            userEmail
                        ) &&
                        registration.UserEmail ==
                            userEmail
                    ) ||
                    (
                        registration.YouthParticipant !=
                            null

                        &&

                        registration.YouthParticipant.UserId
                            ==
                            userId
                    );

                if (!ownsRegistration)
                {
                    return Forbid();
                }
            }


            // ------------------------------------------------------
            // SET LAYOUT TO NULL FOR STANDALONE PRINT VIEW
            // ======================================================

            ViewData["Title"] =
                $"Certificate {certificate.CertificateNumber}";

            return View(certificate);
        }


        // ==========================================================
        // DOWNLOAD - SERVE AS ATTACHMENT
        // 
        // Serves the same print-ready HTML as a downloadable file.
        // The user can then open it and use Print to PDF.
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Download(
            int id)
        {
            var certificate =
                await _context.Certificates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        c =>
                            c.Id == id
                    );

            if (certificate == null)
            {
                return NotFound();
            }


            // ------------------------------------------------------
            // AUTHORIZATION (same as Details)
            // ------------------------------------------------------

            var isAdmin =
                User.IsInRole("Admin");

            if (!isAdmin)
            {
                var userId =
                    User.FindFirst(
                        ClaimTypes.NameIdentifier
                    )?.Value;

                var userEmail =
                    User.Identity?.Name;

                if (string.IsNullOrWhiteSpace(
                    userId
                ))
                {
                    return Unauthorized();
                }

                var registration =
                    await _context
                        .ProgramRegistrations
                        .AsNoTracking()
                        .Include(
                            r =>
                                r.YouthParticipant
                        )
                        .FirstOrDefaultAsync(
                            r =>
                                r.Id ==
                                certificate.RegistrationId
                        );

                if (registration == null)
                {
                    return NotFound();
                }

                // Ownership via UserId, email, or the linked
                // youth participant record (legacy rows where
                // the registration was stored before UserId
                // was filled in).
                var ownsRegistration =
                    registration.UserId ==
                        userId ||
                    (
                        !string.IsNullOrWhiteSpace(
                            userEmail
                        ) &&
                        registration.UserEmail ==
                            userEmail
                    ) ||
                    (
                        registration.YouthParticipant !=
                            null

                        &&

                        registration.YouthParticipant.UserId
                            ==
                            userId
                    );

                if (!ownsRegistration)
                {
                    return Forbid();
                }
            }


            // ------------------------------------------------------
            // BUILD HTML CONTENT FOR DOWNLOAD
            // 
            // Serve as text/html with Content-Disposition attachment.
            // User can open and Print to PDF.
            // ======================================================

            var html =
                GenerateStandaloneHtml(
                    certificate
                );

            var fileName =
                $"{certificate.CertificateNumber}.html";

            return File(
                Encoding.UTF8.GetBytes(
                    html
                ),
                "text/html",
                fileName
            );
        }


        // ==========================================================
        // YOUTH - MY CERTIFICATES
        // 
        // Lists all certificates issued to the current user.
        // ==========================================================

        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> MyCertificates()
        {
            var userId =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;

            var userEmail =
                User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(
                userId
            ))
            {
                return Unauthorized();
            }


            // ------------------------------------------------------
            // FIND ALL REGISTRATIONS OWNED BY THIS USER
            // ======================================================

            var myRegistrations =
                await _context.ProgramRegistrations
                    .AsNoTracking()
                    .Where(
                        r =>
                            r.UserId ==
                                userId ||
                            (
                                !string.IsNullOrWhiteSpace(
                                    userEmail
                                ) &&
                                r.UserEmail ==
                                    userEmail
                            ) ||
                            (
                                r.YouthParticipant !=
                                    null

                                &&

                                r.YouthParticipant.UserId
                                    ==
                                    userId
                            )
                    )
                    .Select(
                        r => r.Id
                    )
                    .ToListAsync();


            // ------------------------------------------------------
            // LOAD CERTIFICATES FOR THESE REGISTRATIONS
            // ======================================================

            var certificates =
                await _context.Certificates
                    .AsNoTracking()
                    .Where(
                        c =>
                            myRegistrations
                                .Contains(
                                    c.RegistrationId
                                )
                    )
                    .OrderByDescending(
                        c => c.IssuedAt
                    )
                    .ToListAsync();


            return View(certificates);
        }


        // ==========================================================
        // HELPER - GENERATE CERTIFICATE NUMBER
        // 
        // Format: BYP-{year}-{8 alphanumeric uppercase chars}
        // Example: BYP-2026-A1B2C3D4
        // ==========================================================

        private static string
            GenerateCertificateNumber()
        {
            var year =
                DateTime.UtcNow.Year;

            const string chars =
                "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // No I, O, 0, 1

            var random =
                new Random();

            var sb =
                new StringBuilder();

            sb.Append(
                $"BYP-{year}-"
            );

            for (int i = 0; i < 8; i++)
            {
                sb.Append(
                    chars[
                        random.Next(
                            chars.Length
                        )]
                );
            }

            return sb.ToString();
        }


        // ==========================================================
        // HELPER - GENERATE STANDALONE HTML
        // 
        // Produces a self-contained HTML page for the certificate,
        // optimized for printing. Used by the Download action.
        // ==========================================================

        private static string
            GenerateStandaloneHtml(
                Certificate cert)
        {
            var completionDate =
                cert.CompletionDate
                    .ToString("MMMM dd, yyyy");

            var issueDate =
                cert.IssuedAt
                    .ToString("MMMM dd, yyyy");

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <title>Certificate {cert.CertificateNumber}</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}

        body {{
            font-family: 'Georgia', 'Times New Roman', serif;
            background: white;
            color: #1a1a1a;
        }}

        .certificate {{
            width: 297mm;
            min-height: 210mm;
            margin: 0 auto;
            padding: 20mm 25mm;
            border: 3pt double #002F2F;
            position: relative;
            background: white;
        }}

        .certificate::before {{
            content: '';
            position: absolute;
            top: 8mm;
            left: 8mm;
            right: 8mm;
            bottom: 8mm;
            border: 1pt solid #009FA3;
        }}

        .header {{
            text-align: center;
            margin-bottom: 15mm;
        }}

        .org-name {{
            font-size: 14pt;
            font-weight: bold;
            color: #002F2F;
            letter-spacing: 3px;
            text-transform: uppercase;
            margin-bottom: 5mm;
        }}

        .cert-title {{
            font-size: 28pt;
            font-weight: bold;
            color: #009FA3;
            margin: 10mm 0 5mm;
            letter-spacing: 5px;
        }}

        .cert-subtitle {{
            font-size: 11pt;
            color: #666;
            font-style: italic;
        }}

        .body {{
            text-align: center;
            margin: 15mm 0;
            line-height: 1.8;
        }}

        .presented-to {{
            font-size: 12pt;
            color: #666;
            margin-bottom: 5mm;
        }}

        .participant-name {{
            font-size: 24pt;
            font-weight: bold;
            color: #002F2F;
            margin: 5mm 0;
            border-bottom: 2pt solid #009FA3;
            display: inline-block;
            padding-bottom: 3mm;
        }}

        .program-text {{
            font-size: 12pt;
            margin: 8mm 0;
            line-height: 1.8;
        }}

        .program-name {{
            font-weight: bold;
            color: #009FA3;
            font-size: 14pt;
        }}

        .completion-date {{
            font-size: 12pt;
            margin: 5mm 0;
        }}

        .footer {{
            display: flex;
            justify-content: space-between;
            margin-top: 20mm;
            padding-top: 10mm;
        }}

        .signature-block {{
            text-align: center;
            width: 40%;
        }}

        .signature-line {{
            border-top: 1pt solid #002F2F;
            padding-top: 3mm;
            margin-bottom: 2mm;
        }}

        .signature-name {{
            font-weight: bold;
            color: #002F2F;
            font-size: 11pt;
        }}

        .signature-role {{
            font-size: 9pt;
            color: #666;
            font-style: italic;
        }}

        .cert-number {{
            position: absolute;
            bottom: 12mm;
            left: 50%;
            transform: translateX(-50%);
            font-size: 9pt;
            color: #999;
            font-family: monospace;
        }}

        @media print {{
            body {{
                margin: 0;
            }}

            .certificate {{
                border: none;
                box-shadow: none;
            }}
        }}
    </style>
</head>
<body>
    <div class=""certificate"">
        <div class=""header"">
            <div class=""org-name"">BYProgram</div>
            <div class=""cert-title"">CERTIFICATE</div>
            <div class=""cert-subtitle"">OF COMPLETION</div>
        </div>

        <div class=""body"">
            <div class=""presented-to"">This is to certify that</div>
            <div class=""participant-name"">{cert.ParticipantName}</div>
            <div class=""program-text"">
                has successfully completed the program<br />
                <span class=""program-name"">{cert.ProgramName}</span>
            </div>
            <div class=""completion-date"">
                on <strong>{completionDate}</strong>
            </div>
        </div>

        <div class=""footer"">
            <div class=""signature-block"">
                <div class=""signature-line"">
                    <div class=""signature-name"">{cert.IssuedByUserName ?? "Program Administrator"}</div>
                    <div class=""signature-role"">Issued By</div>
                </div>
            </div>
            <div class=""signature-block"">
                <div class=""signature-line"">
                    <div class=""signature-name"">{issueDate}</div>
                    <div class=""signature-role"">Date Issued</div>
                </div>
            </div>
        </div>

        <div class=""cert-number"">Certificate No: {cert.CertificateNumber}</div>
    </div>
</body>
</html>";
        }
    }


    // ==========================================================
    // VIEW MODELS
    // ==========================================================

    public class CertificateEligibilityViewModel
    {
        public int RegistrationId { get; set; }

        public string ParticipantName { get; set; } = string.Empty;

        public string? ParticipantEmail { get; set; }

        public int ProgramId { get; set; }

        public string ProgramName { get; set; } = string.Empty;

        public int PresentCount { get; set; }

        public int TotalAttendanceRecords { get; set; }

        public bool HasCertificate { get; set; }

        public string? CertificateNumber { get; set; }

        public DateTime? CertificateIssuedAt { get; set; }
    }


    public class IssueCertificateViewModel
    {
        public int RegistrationId { get; set; }

        public string ParticipantName { get; set; } = string.Empty;

        public string? ParticipantEmail { get; set; }

        public int ProgramId { get; set; }

        public string ProgramName { get; set; } = string.Empty;

        public DateTime? ProgramScheduleEnd { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Completion Date")]
        public DateTime? CompletionDate { get; set; }

        [Display(Name = "I confirm that this participant has completed all program requirements")]
        public bool ConfirmCompletion { get; set; }
    }
}
