using BYPManagementSystem.Models;

namespace BYPManagementSystem.Services
{
    // ============================================================
    // ATTENDANCE SCHEDULE HELPERS
    // ============================================================
    //
    // Central application-time-zone policy for the attendance
    // module:
    //
    // - Window evaluation (inclusive bounds).
    // - Timestamp capture and display conversion.
    // - Action naming (TimeIn / TimeOut).
    //
    // The application time zone is configurable through the
    // "AttendanceTime:TimeZoneId" setting (appsettings.json).
    // When no setting exists, the zone defaults to Asia/Manila
    // (UTC+8, no daylight saving) - the documented policy for
    // this system. Every window check, timestamp conversion and
    // user-facing label goes through this one class, so the
    // configured zone applies consistently to validation,
    // announcements, Photo Review and Reports.
    //
    // Stored timestamps remain UTC like the rest of the system;
    // they are only converted to the application zone for window
    // checks and display.
    //

    public static class AttendanceSchedule
    {
        // ========================================================
        // APPLICATION TIME ZONE
        // Configured once at startup (Program.cs). A fixed custom
        // UTC+8 zone is used as fallback when the ICU zone id is
        // unavailable on the host.
        // ========================================================

        private const string DefaultTimeZoneId = "Asia/Manila";


        private static readonly object ConfigLock =
            new object();


        private static TimeZoneInfo appTimeZone =
            ResolveTimeZone(DefaultTimeZoneId);


        private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }

            // The only fallback that is safe for an unknown id is
            // the documented default offset (Asia/Manila = UTC+8).

            return TimeZoneInfo.CreateCustomTimeZone(
                timeZoneId,
                TimeSpan.FromHours(8),
                timeZoneId,
                timeZoneId);
        }


        // ========================================================
        // STARTUP CONFIGURATION
        // Called once from Program.cs before any request or the
        // announcement background service runs.
        // ========================================================

        public static void Configure(string? timeZoneId)
        {
            var id =
                string.IsNullOrWhiteSpace(timeZoneId)
                    ? DefaultTimeZoneId
                    : timeZoneId.Trim();

            lock (ConfigLock)
            {
                appTimeZone =
                    ResolveTimeZone(id);
            }
        }


        // ========================================================
        // ZONE LABEL FOR USER-FACING MESSAGES
        // "Philippine time" is only used when the application is
        // actually running in Asia/Manila. Any other configured
        // zone is labeled with its real id, so messages never
        // claim Philippine time while validating against a
        // different zone.
        // ========================================================

        public static string ZoneLabel =>
            string.Equals(
                appTimeZone.Id,
                DefaultTimeZoneId,
                StringComparison.OrdinalIgnoreCase)
                    ? "Philippine time"
                    : appTimeZone.Id;


        // ========================================================
        // CURRENT APPLICATION-ZONE TIME
        // ========================================================

        public static DateTime NowApp() =>
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                appTimeZone);


        // ========================================================
        // UTC -> APPLICATION ZONE
        // Stored timestamps are UTC. Values of unknown kind are
        // treated as UTC because that is how this system stores
        // every server-recorded timestamp.
        // ========================================================

        public static DateTime ToAppTime(DateTime value)
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
                appTimeZone);
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
        // WINDOW STATUS LABEL
        // "Not Open Yet" / "Open Now" / "Closed" for display.
        // A window in a program that is not scheduled on the
        // checked date is always Closed - a program scheduled
        // for another date never opens attendance today.
        // ========================================================

        public static string WindowStatus(
            bool scheduledOnDate,
            TimeOnly now,
            TimeOnly start,
            TimeOnly end)
        {
            if (!scheduledOnDate)
            {
                return "Closed";
            }

            var minute =
                new TimeOnly(now.Hour, now.Minute);

            if (minute < start)
            {
                return "Not Open Yet";
            }

            if (minute > end)
            {
                return "Closed";
            }

            return "Open Now";
        }


        // ========================================================
        // PROGRAM SCHEDULE DATE ELIGIBILITY
        // ========================================================
        //
        // A program is scheduled on an application-zone date when
        // its optional structured schedule range (ScheduleStart /
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
            DateTime appDate)
        {
            if (HasInvalidSchedule(program))
            {
                return false;
            }

            var date = appDate.Date;


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
            "Morning In "
            + FormatTime(program.TimeInStart)
            + " \u2013 "
            + FormatTime(program.TimeInEnd)
            + " \u00b7 Morning Out "
            + FormatTime(program.TimeOutStart)
            + " \u2013 "
            + FormatTime(program.TimeOutEnd)
            + " \u00b7 Afternoon In "
            + FormatTime(program.AfternoonInStart)
            + " \u2013 "
            + FormatTime(program.AfternoonInEnd)
            + " \u00b7 Afternoon Out "
            + FormatTime(program.AfternoonOutStart)
            + " \u2013 "
            + FormatTime(program.AfternoonOutEnd);


        // ========================================================
        // ACTION NAMING (FOUR ACTIONS)
        // ========================================================
        //
        // Canonical stored values:
        //
        //   "MorningIn"    Morning Time In
        //   "MorningOut"   Morning Time Out
        //   "AfternoonIn"  Afternoon Time In
        //   "AfternoonOut" Afternoon Time Out
        //
        // Legacy submissions that predate the four-action model
        // stored "TimeIn" / "TimeOut" (the old single-session day).
        // They are accepted everywhere as Morning In / Morning Out.
        // Missing or unknown values fall back to MorningIn for
        // backward compatibility with the oldest rows.
        // ========================================================

        public const string MorningInAction = "MorningIn";
        public const string MorningOutAction = "MorningOut";
        public const string AfternoonInAction = "AfternoonIn";
        public const string AfternoonOutAction = "AfternoonOut";


        public static readonly string[] AllActions =
        {
            MorningInAction,
            MorningOutAction,
            AfternoonInAction,
            AfternoonOutAction
        };


        public static bool EqualsCanonical(
            string? action,
            string canonical) =>
            NormalizeAction(action) == canonical;


        public static bool IsMorningAction(string? action) =>
            !IsAfternoonAction(action);


        public static bool IsAfternoonAction(string? action)
        {
            switch (NormalizeAction(action))
            {
                case AfternoonInAction:
                case AfternoonOutAction:
                    return true;
                default: return false;
            }
        }


        // IsTimeOutAction now means "a Time Out of any session".
        // Kept for the view styling and rule checks that only
        // care about in vs. out.

        public static bool IsTimeOutAction(string? action)
        {
            switch (NormalizeAction(action))
            {
                case MorningOutAction:
                case AfternoonOutAction:
                    return true;
                default: return false;
            }
        }


        public static bool IsTimeInAction(string? action) =>
            !IsTimeOutAction(action);


        public static string NormalizeAction(string? action)
        {
            switch (action?.Trim())
            {
                case "AfternoonIn":
                    return AfternoonInAction;

                case "AfternoonOut":
                    return AfternoonOutAction;

                // Legacy single-session values map to the
                // morning session.
                case "MorningOut":
                case "TimeOut":
                    return MorningOutAction;

                // "MorningIn", "TimeIn", null and anything
                // unknown fall back to Morning In, which is what
                // legacy "TimeIn" rows mean.
                default:
                    return MorningInAction;
            }
        }


        public static string ActionLabel(string? action)
        {
            switch (NormalizeAction(action))
            {
                case MorningOutAction:
                    return "Morning Time Out";
                case AfternoonInAction:
                    return "Afternoon Time In";
                case AfternoonOutAction:
                    return "Afternoon Time Out";
                default:
                    return "Morning Time In";
            }
        }


        // ========================================================
        // WINDOW BOUNDS FOR AN ACTION
        // All four actions resolve through this one helper, so
        // submission validation, photo approval and announcements
        // always agree on which saved range applies.
        // ========================================================

        public static (TimeOnly Start, TimeOnly End) WindowFor(
            YouthProgram program,
            string action)
        {
            switch (NormalizeAction(action))
            {
                case MorningOutAction:
                    return (program.TimeOutStart,
                        program.TimeOutEnd);

                case AfternoonInAction:
                    return (program.AfternoonInStart,
                        program.AfternoonInEnd);

                case AfternoonOutAction:
                    return (program.AfternoonOutStart,
                        program.AfternoonOutEnd);

                default:
                    return (program.TimeInStart,
                        program.TimeInEnd);
            }
        }


        // ========================================================
        // SESSION IN / OUT PAIRING
        //
        // A Time Out always belongs to the same session as its
        // Time In: Morning Out requires Morning In, Afternoon Out
        // requires Afternoon In. The In action of one session is
        // not dependent on the other session.
        // ========================================================

        public static string SessionInFor(string outputAction) =>
            NormalizeAction(outputAction) switch
            {
                MorningOutAction => MorningInAction,
                AfternoonOutAction => AfternoonInAction,
                var inAction => inAction
            };


        // ========================================================
        // ATTENDANCE RECORD TIMES
        // Read and write the correct time column (UTC) for an
        // action, so submission and approval logic never need to
        // know the column names.
        // ========================================================

        public static DateTime? RecordedTimeFor(
            AttendanceRecord record,
            string action)
        {
            switch (NormalizeAction(action))
            {
                case MorningOutAction:
                    return record.TimeOutAt;
                case AfternoonInAction:
                    return record.AfternoonInAt;
                case AfternoonOutAction:
                    return record.AfternoonOutAt;
                default:
                    return record.TimeInAt;
            }
        }


        public static int RecordedCount(AttendanceRecord record)
        {
            var count = 0;

            foreach (var action in AllActions)
            {
                if (RecordedTimeFor(record, action)
                        .HasValue)
                {
                    count++;
                }
            }

            return count;
        }


        // Writes the correct UTC column for an action. Used only
        // from the photo approver, so approval can never write a
        // submission under the wrong session.

        public static void SetRecordedTime(
            AttendanceRecord record,
            string action,
            DateTime valueUtc)
        {
            switch (NormalizeAction(action))
            {
                case MorningOutAction:
                    record.TimeOutAt = valueUtc;
                    break;

                case AfternoonInAction:
                    record.AfternoonInAt = valueUtc;
                    break;

                case AfternoonOutAction:
                    record.AfternoonOutAt = valueUtc;
                    break;

                default:
                    record.TimeInAt = valueUtc;
                    break;
            }
        }


        // Session status for one in/out pair.
        // Pending -> In Progress -> Complete.

        public static string SessionStatus(
            bool inRecorded,
            bool outRecorded)
        {
            if (outRecorded)
            {
                return "Complete";
            }

            return inRecorded ? "In Progress" : "Pending";
        }


        // Morning session status label.

        public static string MorningStatus(
            AttendanceRecord record) =>
            SessionStatus(
                record.TimeInAt.HasValue,
                record.TimeOutAt.HasValue);


        // Afternoon session status label.

        public static string AfternoonStatus(
            AttendanceRecord record) =>
            SessionStatus(
                record.AfternoonInAt.HasValue,
                record.AfternoonOutAt.HasValue);


        // Full-day status across all four actions. "Present" only
        // once the afternoon Time Out closes the day; any earlier
        // stage stays "In Progress".

        public static string DayStatus(AttendanceRecord record) =>
            record.TimeInAt.HasValue
                && record.TimeOutAt.HasValue
                && record.AfternoonInAt.HasValue
                && record.AfternoonOutAt.HasValue
                    ? "Present"
                    : (RecordedCount(record) > 0
                        ? "In Progress"
                        : "Pending");
    }
}
