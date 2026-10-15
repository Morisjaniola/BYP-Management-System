using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BYPManagementSystem.Models
{
    [Index(nameof(RegistrationId), nameof(AttendanceDate), IsUnique = true)]
    public class AttendanceRecord
    {
        public int Id { get; set; }

        // Approved registration this attendance belongs to
        [Required]
        public int RegistrationId { get; set; }

        public ProgramRegistration? Registration { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Attendance Date")]
        public DateTime AttendanceDate { get; set; } =
            DateTime.Today;

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Present";


        // ==========================================================
        // APPROVED TIME IN / TIME OUT (UTC)
        //
        // Filled only from approved photo submissions, always
        // using the original validated server-recorded submission
        // time - never the later approval time, and never typed
        // in manually. Null means not yet recorded.
        // ==========================================================

        public DateTime? TimeInAt { get; set; }

        public DateTime? TimeOutAt { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime RecordedAt { get; set; } =
            DateTime.UtcNow;
    }
}