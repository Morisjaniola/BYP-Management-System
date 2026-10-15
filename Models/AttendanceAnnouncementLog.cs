using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BYPManagementSystem.Models
{
    // ============================================================
    // ATTENDANCE ANNOUNCEMENT LOG
    // ============================================================
    //
    // One row per schedule announcement that was actually sent
    // (per program, per attendance date, per announcement kind).
    //
    // The unique index on ProgramId + AttendanceDate + Kind makes
    // the scheduled announcement service idempotent: a trigger
    // can only ever fire once, no matter how often the service
    // ticks, the server restarts, or pages are refreshed.
    //

    [Index(
        nameof(ProgramId),
        nameof(AttendanceDate),
        nameof(Kind),
        IsUnique = true)]
    public class AttendanceAnnouncementLog
    {
        public int Id { get; set; }


        // ==========================================================
        // PROGRAM
        // Stored without a foreign key so historical log rows
        // survive program deletion.
        // ==========================================================

        public int ProgramId { get; set; }


        // ==========================================================
        // ATTENDANCE DATE (MANILA, DATE PART ONLY)
        // ==========================================================

        [Required]
        [DataType(DataType.Date)]
        public DateTime AttendanceDate { get; set; }


        // ==========================================================
        // KIND
        // TimeInOpen | TimeInSoon | TimeOutOpen | TimeOutSoon
        // ==========================================================

        [Required]
        [StringLength(30)]
        public string Kind { get; set; } = string.Empty;


        public DateTime SentAt { get; set; } =
            DateTime.UtcNow;
    }
}
