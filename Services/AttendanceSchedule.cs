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
        //
        // Boundaries are inclusive at minute precision, which is
        // the precision the windows are configured with: a Time
        // In window of 7:00 - 8:30 accepts anything from
        // 7:00:00 through 8:30:59, and 6:59:59 is rejected.
        // The submission and approval paths both call this one
        // helper, so the policy can never drift between them.
        //
        // The checked value is only used for this comparison;
        // the recorded attendance timestamp always stays the
        // exact server-captured submission time.
        //

        public static bool IsWithin(
            TimeOnly now,
            TimeOnly start,
            TimeOnly end)
        {
            var minute =
                new TimeOnly(now.Hour, now.Minute);

            return minute >= start && minute <= end;
        }


        // ========================================================
        // PROGRAM SCHEDULE DATE ELIGIBILITY
        // ========================================================
        //
        // A program is scheduled on a Manila date when its
        // optional structured schedule range (ScheduleStart /
        // ScheduleEnd) covers that date. Programs that only
        // have the free-text Schedule (no structured dates)
        // remain attendable on any date - that is the
        // documented legacy policy. A structured range whose
        // start date is after its end date is invalid and
        // never scheduled.
        //

        public static bool HasInvalidSchedule(
            YouthProgram program) =>
            program.ScheduleStart.HasValue &&
            program.ScheduleEnd.HasValue &&
            program.ScheduleStart.Value.Date >
                program.ScheduleEnd.Value.Date;


        public static bool IsScheduledOn(
            YouthProgram program,
            DateTime manilaDate)
        {
            if (HasInvalidSchedule(program))
            {
                return false;
            }

            var date = manilaDate.Date;


            if (program.ScheduleStart.HasValue &&
                program.ScheduleStart.Value.Date > date)
            {
                // The program has not started yet.
                return false;
            }


            if (program.ScheduleEnd.HasValue &&
                program.ScheduleEnd.Value.Date < date)
            {
                // The program already ended.
                return false;
            }


            return true;
        }


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
