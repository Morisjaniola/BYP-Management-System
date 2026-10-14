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

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime RecordedAt { get; set; } =
            DateTime.UtcNow;
    }
}