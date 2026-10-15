using System.ComponentModel.DataAnnotations;

namespace BYPManagementSystem.Models
{
    public class YouthProgram
    {
        public int Id { get; set; }

        // ==========================================================
        // PROGRAM NAME
        // ==========================================================

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;


        // ==========================================================
        // DESCRIPTION
        // ==========================================================

        [StringLength(1000)]
        public string? Description { get; set; }


        // ==========================================================
        // SCHEDULE
        // ==========================================================

        [Required]
        [StringLength(250)]
        public string Schedule { get; set; } = string.Empty;


        // ==========================================================
        // SCHEDULE START DATE/TIME
        //
        // Optional structured start of the program schedule.
        // Selected through a date/time picker. Nullable so that
        // legacy programs that only have the free-text Schedule
        // remain valid and readable.
        // ==========================================================

        public DateTime? ScheduleStart { get; set; }


        // ==========================================================
        // SCHEDULE END DATE/TIME
        //
        // Optional structured end of the program schedule.
        // Must be later than ScheduleStart when both are set.
        // ==========================================================

        public DateTime? ScheduleEnd { get; set; }


        // ==========================================================
        // ATTENDANCE WINDOWS (MORNING SCHEDULE)
        //
        // Allowed submission windows in Philippine time
        // (Asia/Manila). Defaults follow the barangay morning
        // schedule: Time In 7:00 AM - 8:30 AM and Time Out
        // 10:30 AM - 11:30 AM. Configurable per program.
        // ==========================================================

        [DataType(DataType.Time)]
        [Display(Name = "Time In Window Start")]
        public TimeOnly TimeInStart { get; set; } =
            new TimeOnly(7, 0);


        [DataType(DataType.Time)]
        [Display(Name = "Time In Window End")]
        public TimeOnly TimeInEnd { get; set; } =
            new TimeOnly(8, 30);


        [DataType(DataType.Time)]
        [Display(Name = "Time Out Window Start")]
        public TimeOnly TimeOutStart { get; set; } =
            new TimeOnly(10, 30);


        [DataType(DataType.Time)]
        [Display(Name = "Time Out Window End")]
        public TimeOnly TimeOutEnd { get; set; } =
            new TimeOnly(11, 30);


        // ==========================================================
        // STATUS
        // ==========================================================

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active";


        // ==========================================================
        // CAPACITY
        // ==========================================================

        [Range(1, 100000)]
        public int Capacity { get; set; } = 50;


        // ==========================================================
        // BARANGAY
        //
        // This is used for automatic youth assignment.
        //
        // Example:
        //
        // Program Barangay = Cansiwang
        //
        // Youth Barangay = Cansiwang
        //
        // They will automatically be connected.
        // ==========================================================

        [Required]
        [StringLength(100)]
        public string Barangay { get; set; } = string.Empty;


        // ==========================================================
        // CREATED DATE
        // ==========================================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}