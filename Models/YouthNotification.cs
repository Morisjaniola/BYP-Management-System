using System.ComponentModel.DataAnnotations;

namespace BYPManagementSystem.Models
{
    // ============================================================
    // YOUTH NOTIFICATION
    // ============================================================
    //

    public class YouthNotification
    {
        public int Id { get; set; }


        // ==========================================================
        // RECIPIENT (IDENTITY USER)
        // ==========================================================

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;


        // ==========================================================
        // CONTENT
        // ==========================================================

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;


        // ==========================================================
        // TYPE / CATEGORY
        // e.g. Registration, Approval, Rejection, Attendance
        // ==========================================================

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "General";


        // ==========================================================
        // CREATED / READ STATE
        // ==========================================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; }


        // ==========================================================
        // OPTIONAL RELATED REFERENCES
        // Stored without foreign keys so historical notifications
        // survive program or registration deletion.
        // ==========================================================

        public int? RegistrationId { get; set; }

        public int? ProgramId { get; set; }
    }
}
