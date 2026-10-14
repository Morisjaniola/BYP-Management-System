using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BYPManagementSystem.Models
{
    // ============================================================
    // ATTENDANCE PHOTO SUBMISSION
    // ============================================================
    //
    // A picture submitted by a Youth (User) as proof of attendance
    // for one of their own approved program registrations.
    //
    // A submission never marks attendance by itself. Attendance is
    // only recorded after an Admin/Staff member approves the photo
    // through the Attendance module review queue.
    //

    [Index(nameof(RegistrationId), nameof(Status))]
    [Index(nameof(RegistrationId), nameof(AttendanceDate))]
    public class AttendancePhotoSubmission
    {
        public int Id { get; set; }


        // ==========================================================
        // REGISTRATION
        // Approved registration this submission belongs to.
        // Links to the participant and program through the
        // registration so historical data survives changes.
        // ==========================================================

        [Required]
        public int RegistrationId { get; set; }

        public ProgramRegistration? Registration { get; set; }


        // ==========================================================
        // ATTENDANCE DATE
        // The date the photo proves attendance for. Assigned on
        // the server at submission time - never from the browser.
        // ==========================================================

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Attendance Date")]
        public DateTime AttendanceDate { get; set; } =
            DateTime.Today;


        // ==========================================================
        // FILE
        // FileName is a server-generated name only (no path
        // components), so it can never traverse outside the
        // configured storage folder.
        // ==========================================================

        [Required]
        [StringLength(260)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }


        // ==========================================================
        // SUBMISSION
        // ==========================================================

        [StringLength(450)]
        public string? SubmittedByUserId { get; set; }

        public DateTime SubmittedAt { get; set; } =
            DateTime.UtcNow;


        // ==========================================================
        // REVIEW
        // Status: Pending | Approved | Rejected
        // ==========================================================

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [StringLength(450)]
        public string? ReviewedByUserId { get; set; }

        [StringLength(256)]
        public string? ReviewedByUserName { get; set; }
    }
}
