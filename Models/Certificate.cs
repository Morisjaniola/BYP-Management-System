using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BYPManagementSystem.Models
{
    // ==========================================================
    // CERTIFICATE
    // 
    // Represents an issued completion certificate for a youth
    // participant who has completed a program. Each certificate
    // is tied to a specific registration (participant + program
    // pair) and has a globally unique certificate number.
    // ==========================================================

    [Index(nameof(CertificateNumber), IsUnique = true)]
    [Index(nameof(RegistrationId), IsUnique = true)] // One certificate per registration
    public class Certificate
    {
        public int Id { get; set; }

        // ==========================================================
        // CERTIFICATE NUMBER
        // 
        // Globally unique identifier for this certificate.
        // Format: BYP-{year}-{8 alphanumeric chars}
        // Example: BYP-2026-A1B2C3D4
        // ==========================================================

        [Required]
        [StringLength(50)]
        public string CertificateNumber { get; set; } = string.Empty;


        // ==========================================================
        // REGISTRATION
        // 
        // Links to the specific registration (participant + program
        // pair). This enforces one certificate per participant per
        // program through the unique index.
        // ==========================================================

        [Required]
        public int RegistrationId { get; set; }

        public ProgramRegistration? Registration { get; set; }


        // ==========================================================
        // PARTICIPANT NAME (SNAPSHOT)
        // 
        // Snapshot of the participant's full name at the time of
        // issuance. This ensures the certificate remains stable
        // even if the participant's name is later corrected.
        // ==========================================================

        [Required]
        [StringLength(200)]
        public string ParticipantName { get; set; } = string.Empty;


        // ==========================================================
        // PROGRAM NAME (SNAPSHOT)
        // 
        // Snapshot of the program name at the time of issuance.
        // ==========================================================

        [Required]
        [StringLength(150)]
        public string ProgramName { get; set; } = string.Empty;


        // ==========================================================
        // COMPLETION DATE
        // 
        // The date the participant completed the program. This is
        // derived from the program's ScheduleEnd date if available,
        // otherwise falls back to the certificate issue date.
        // This field is required because the certificate confirms
        // completion on a specific date.
        // ==========================================================

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Completion Date")]
        public DateTime CompletionDate { get; set; }


        // ==========================================================
        // ISSUED AT
        // 
        // Timestamp when the certificate was issued (UTC).
        // ==========================================================

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;


        // ==========================================================
        // ISSUED BY (USER ID)
        // 
        // The admin user who issued this certificate. Stored as a
        // string to match the IdentityUser.Id type (GUID string).
        // ==========================================================

        [StringLength(450)]
        public string? IssuedByUserId { get; set; }


        // ==========================================================
        // ISSUED BY (USER NAME)
        // 
        // Snapshot of the issuing admin's display name at the time
        // of issuance. Used for display purposes if the admin
        // account is later removed.
        // ==========================================================

        [StringLength(200)]
        public string? IssuedByUserName { get; set; }
    }
}
