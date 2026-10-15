using BYPManagementSystem.Models;

namespace BYPManagementSystem.Services
{
    // ============================================================
    // ATTENDANCE SCHEDULE HELPERS
    // ============================================================
    //
    // Central Philippine-time (Asia/Manila) policy for the
    // attendance module:
    //
    // - Window evaluation (inclusive bounds).
    // - Timestamp capture and display conversion.
    // - Action naming (TimeIn / TimeOut).
    //
    // Stored timestamps remain UTC like the rest of the system;
    // they are only converted to Manila time for window checks
    // and display.
    //

    public static class AttendanceSchedule
    {
        // ========================================================
        // TIME ZONE
        // Asia/Manila is UTC+8 with no daylight saving. A fixed
        // custom zone is used as fallback when the ICU zone id is
        // unavailable on the host.
        // ========================================================

        private static readonly TimeZoneInfo ManilaTimeZone =
            ResolveManilaTimeZone();


        private static TimeZoneInfo ResolveManilaTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "Asia/Manila");
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }

            return TimeZoneInfo.CreateCustomTimeZone(
                "Asia/Manila",
                TimeSpan.FromHours(8),
                "Asia/Manila",
                "Asia/Manila");
        }


        // ========================================================
        // CURRENT MANILA TIME
        // ========================================================

        public static DateTime NowManila() =>
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                ManilaTimeZone);


        // ========================================================
        // UTC -> MANILA
        // Stored timestamps are UTC. Values of unknown kind are
        // treated as UTC because that is how this system stores
        // every server-recorded timestamp.
        // ========================================================

        public static DateTime ToManila(DateTime value)
        {
            var utc =
                value.Kind switch
                {
                    DateTimeKind.Utc => value,
                    DateTimeKind.Local => value.ToUniversalTime(),
                    _ => DateTime.SpecifyKind(
                        value,
                        DateTimeKind.Utc)
                };

            return TimeZoneInfo.ConvertTimeFromUtc(
                utc,
                ManilaTimeZone);
        }


        // ========================================================
        // WINDOW EVALUATION (INCLUSIVE BOUNDS)
        // ========================================================

        public static bool IsWithin(
            TimeOnly now,
            TimeOnly start,
            TimeOnly end) =>
            now >= start && now <= end;


        // ========================================================
        // FORMATTING
        // ========================================================

        public static string FormatTime(TimeOnly time) =>
            time.ToString("h:mm tt");


        public static string WindowsText(YouthProgram program) =>
            "Time In "
            + FormatTime(program.TimeInStart)
            + " \u2013 "
            + FormatTime(program.TimeInEnd)
            + " \u00b7 Time Out "
            + FormatTime(program.TimeOutStart)
            + " \u2013 "
            + FormatTime(program.TimeOutEnd);


        // ========================================================
        // ACTION NAMING
        // "TimeIn" and "TimeOut". Missing or unknown values fall
        // back to TimeIn for backward compatibility with legacy
        // submissions that predate the action field.
        // ========================================================

        public static bool IsTimeOutAction(string? action) =>
            string.Equals(
                action,
                "TimeOut",
                StringComparison.OrdinalIgnoreCase);


        public static bool IsTimeInAction(string? action) =>
            !IsTimeOutAction(action);


        public static string NormalizeAction(string? action) =>
            IsTimeOutAction(action) ? "TimeOut" : "TimeIn";


        public static string ActionLabel(string? action) =>
            IsTimeOutAction(action) ? "Time Out" : "Time In";


        // ========================================================
        // WINDOW BOUNDS FOR AN ACTION
        // ========================================================

        public static (TimeOnly Start, TimeOnly End) WindowFor(
            YouthProgram program,
            string action) =>
            IsTimeOutAction(action)
                ? (program.TimeOutStart, program.TimeOutEnd)
                : (program.TimeInStart, program.TimeInEnd);
    }
}
