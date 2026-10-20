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
        //
        // Legacy free-text description. The Create/Edit forms no
        // longer offer an input for it; the controller generates
        // this text from the structured start/end pickers so
        // existing displays, searches, and saved records keep
        // working unchanged.
        // ==========================================================

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
        // ATTENDANCE WINDOWS (MORNING SESSION)
        //
        // Allowed submission windows in the application time zone
        // (Asia/Manila by default - see AttendanceTime in
        // appsettings.json). Defaults follow the barangay default
        // schedule: Morning Time In 7:00 AM - 8:30 AM and Morning
        // Time Out 10:30 AM - 11:30 AM. Configurable per program.
        //
        // These two windows are the morning session of the
        // four-window schedule; a legacy program whose rows were
        // created before the afternoon columns existed keeps its
        // saved morning times untouched.
        // ==========================================================

        [DataType(DataType.Time)]
        [Display(Name = "Morning Time In Start")]
        public TimeOnly TimeInStart { get; set; } =
            new TimeOnly(7, 0);


        [DataType(DataType.Time)]
        [Display(Name = "Morning Time In End")]
        public TimeOnly TimeInEnd { get; set; } =
            new TimeOnly(8, 30);


        [DataType(DataType.Time)]
        [Display(Name = "Morning Time Out Start")]
        public TimeOnly TimeOutStart { get; set; } =
            new TimeOnly(10, 30);


        [DataType(DataType.Time)]
        [Display(Name = "Morning Time Out End")]
        public TimeOnly TimeOutEnd { get; set; } =
            new TimeOnly(11, 30);


        // ==========================================================
        // ATTENDANCE WINDOWS (AFTERNOON SESSION)
        //
        // The second half of the four-window attendance
        // schedule: Afternoon Time In 1:00 PM - 2:00 PM and
        // Afternoon Time Out 4:00 PM - 5:00 PM by default,
        // editable per program like the morning times.
        // ==========================================================

        [DataType(DataType.Time)]
        [Display(Name = "Afternoon Time In Start")]
        public TimeOnly AfternoonInStart { get; set; } =
            new TimeOnly(13, 0);


        [DataType(DataType.Time)]
        [Display(Name = "Afternoon Time In End")]
        public TimeOnly AfternoonInEnd { get; set; } =
            new TimeOnly(14, 0);


        [DataType(DataType.Time)]
        [Display(Name = "Afternoon Time Out Start")]
        public TimeOnly AfternoonOutStart { get; set; } =
            new TimeOnly(16, 0);


        [DataType(DataType.Time)]
        [Display(Name = "Afternoon Time Out End")]
        public TimeOnly AfternoonOutEnd { get; set; } =
            new TimeOnly(17, 0);


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