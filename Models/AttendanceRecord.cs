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
        // APPROVED ATTENDANCE TIMES (UTC)
        //
        // Filled only from approved photo submissions, always
        // using the original validated server-recorded submission
        // time - never the later approval time, and never typed
        // in manually. Null means not yet recorded.
        //
        // TimeInAt / TimeOutAt are the morning session; the
        // afternoon columns were added with the four-window
        // schedule. Legacy records only ever have morning times.
        // ==========================================================

        public DateTime? TimeInAt { get; set; }

        public DateTime? TimeOutAt { get; set; }

        public DateTime? AfternoonInAt { get; set; }

        public DateTime? AfternoonOutAt { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime RecordedAt { get; set; } =
            DateTime.UtcNow;
    }
}