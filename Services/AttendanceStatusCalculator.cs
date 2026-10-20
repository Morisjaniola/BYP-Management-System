using BYPManagementSystem.Models;

namespace BYPManagementSystem.Services
{
    // ============================================================
    // ATTENDANCE STATUS CALCULATOR
    //
    // Computes the per-action and overall attendance status of an
    // approved registration for ONE attendance date from:
    //   - the program's configured attendance windows,
    //   - the approved attendance record (if any) for that date,
    //   - that date's photo submissions (Pending/Approved/Rejected).
    //
    // Overall status priority (first match wins):
    //   1. Pending Review - a submission awaits review. A participant
    //      is NEVER marked Absent/Incomplete while pending.
    //   2. Present - all four actions are approved.
    //   3. Incomplete - an approved action exists but its required
    //      paired action is missing after that window ended.
    //   4. Rejected - a submission was rejected and not replaced.
    //   5. Absent - the applicable window ended with no valid
    //      submission and nothing pending.
    //   6. Not Yet Due - the window has not ended / date not reached.
    //
    // Morning and afternoon sessions are evaluated independently;
    // completing the morning never completes the afternoon.
    //
    // A staff-created attendance record with NO photo-approved times
    // (manual entry: Present/Late/Excused/Absent) keeps its own
    // status - manual entries remain authoritative and are never
    // overwritten by the schedule computation.
    // ============================================================

    public static class AttendanceStatusCalculator
    {
        public const string KindApproved = "Approved";
        public const string KindPending = "Pending";
        public const string KindRejected = "Rejected";
        public const string KindNone = "None";

        public const string PendingReview = "Pending Review";
        public const string Present = "Present";
        public const string Incomplete = "Incomplete";
        public const string Rejected = "Rejected";
        public const string Absent = "Absent";
        public const string NotYetDue = "Not Yet Due";


        // State of ONE attendance action for one date.

        public sealed class ActionState
        {
            public string Action { get; init; } =
                AttendanceSchedule.MorningInAction;

            // Approved | Pending | Rejected | None

            public string Kind { get; init; } = KindNone;

            // App-time timestamp of the approved submission, when
            // Kind == Approved. Never the window boundary time.

            public DateTime? ApprovedTimeApp { get; init; }
        }


        public sealed class DayEvaluation
        {
            public ActionState MorningIn { get; init; } = new();
            public ActionState MorningOut { get; init; } = new();
            public ActionState AfternoonIn { get; init; } = new();
            public ActionState AfternoonOut { get; init; } = new();

            public string MorningStatus { get; init; } = NotYetDue;
            public string AfternoonStatus { get; init; } = NotYetDue;
            public string OverallStatus { get; init; } = NotYetDue;
        }


        public static DayEvaluation Evaluate(
            YouthProgram program,
            AttendanceRecord? record,
            IReadOnlyCollection<AttendancePhotoSubmission>? submissions,
            DateTime appDate,
            DateTime appNow)
        {
            appDate = appDate.Date;

            var nowDate = appNow.Date;
            var nowTime = TimeOnly.FromDateTime(appNow);

            submissions ??= Array.Empty<AttendancePhotoSubmission>();


            // A window has ended when the whole attendance date is in
            // the past, or (for today) the current time is past the
            // window end. Window bounds are inclusive on both sides.

            bool Ended(TimeOnly windowEnd) =>
                appDate < nowDate ||
                (appDate == nowDate && nowTime > windowEnd);


            // Per-action state: an approved time on the record wins;
            // otherwise a pending submission beats a rejected one.

            ActionState Build(string action)
            {
                var approvedUtc = record == null
                    ? null
                    : AttendanceSchedule.RecordedTimeFor(record, action);

                var kind = KindNone;

                if (approvedUtc.HasValue)
                {
                    kind = KindApproved;
                }
                else
                {
                    foreach (var submission in submissions)
                    {
                        if (!AttendanceSchedule.EqualsCanonical(
                                submission.Action,
                                action))
                        {
                            continue;
                        }

                        if (string.Equals(
                                submission.Status,
                                "Pending",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            kind = KindPending;
                            break;
                        }

                        if (string.Equals(
                                submission.Status,
                                "Rejected",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            kind = KindRejected;
                        }
                    }
                }

                return new ActionState
                {
                    Action = action,
                    Kind = kind,
                    ApprovedTimeApp = approvedUtc.HasValue
                        ? AttendanceSchedule.ToAppTime(approvedUtc.Value)
                        : null
                };
            }


            var morningIn = Build(AttendanceSchedule.MorningInAction);
            var morningOut = Build(AttendanceSchedule.MorningOutAction);
            var afternoonIn = Build(AttendanceSchedule.AfternoonInAction);
            var afternoonOut = Build(AttendanceSchedule.AfternoonOutAction);


            // One session = its In action + its Out action.

            string Session(
                ActionState inState,
                ActionState outState,
                TimeOnly outWindowEnd)
            {
                // Pending outranks everything else. Never Absent
                // while a relevant submission awaits review.

                if (inState.Kind == KindPending ||
                    outState.Kind == KindPending)
                {
                    return PendingReview;
                }

                var inApproved = inState.Kind == KindApproved;
                var outApproved = outState.Kind == KindApproved;

                // Both required actions approved -> session done.

                if (inApproved && outApproved)
                {
                    return Present;
                }

                var outEnded = Ended(outWindowEnd);

                // Some approved action exists.

                if (inApproved || outApproved)
                {
                    // Approved In but the required Out is missing
                    // after its window closed -> Incomplete.

                    return outEnded ? Incomplete : NotYetDue;
                }

                // Nothing approved.

                if (outEnded)
                {
                    // The window closed with only rejected attempts
                    // -> surface the rejection; otherwise Absent.

                    if (inState.Kind == KindRejected ||
                        outState.Kind == KindRejected)
                    {
                        return Rejected;
                    }

                    return Absent;
                }

                // Window still open or date not reached.

                return NotYetDue;
            }

            var morningStatus = Session(
                morningIn,
                morningOut,
                program.TimeOutEnd);

            var afternoonStatus = Session(
                afternoonIn,
                afternoonOut,
                program.AfternoonOutEnd);


            string overall;

            // --------------------------------------------------
            // OVERALL PRIORITY
            //
            // 1. Pending Review outranks every other outcome -
            //    including a staff manual record. While a
            //    relevant submission awaits review the day must
            //    never be finalized as Present or Absent.
            // 2. The day is not finalized while either session
            //    is still Not Yet Due; the session columns carry
            //    the granular state in the meantime.
            // 3. A staff manual entry without any photo-approved
            //    times keeps its recorded status.
            // 4. Otherwise the sessions combine as
            //    Present > Incomplete > Rejected > Absent.
            // --------------------------------------------------

            if (morningStatus == PendingReview ||
                afternoonStatus == PendingReview)
            {
                overall = PendingReview;
            }
            else if (morningStatus == NotYetDue ||
                     afternoonStatus == NotYetDue)
            {
                overall = NotYetDue;
            }
            else if (record != null &&
                AttendanceSchedule.RecordedCount(record) == 0 &&
                record.Status is "Present" or "Late" or "Excused"
                    or "Absent")
            {
                // Staff manual entry without any photo-approved
                // times keeps its recorded status.

                overall = record.Status;
            }
            else if (morningStatus == Present &&
                     afternoonStatus == Present)
            {
                overall = Present;
            }
            else if (morningStatus == Incomplete ||
                     afternoonStatus == Incomplete)
            {
                overall = Incomplete;
            }
            else if (morningStatus == Rejected ||
                     afternoonStatus == Rejected)
            {
                overall = Rejected;
            }
            else if (morningStatus == Absent ||
                     afternoonStatus == Absent)
            {
                overall = Absent;
            }
            else
            {
                overall = NotYetDue;
            }

            return new DayEvaluation
            {
                MorningIn = morningIn,
                MorningOut = morningOut,
                AfternoonIn = afternoonIn,
                AfternoonOut = afternoonOut,
                MorningStatus = morningStatus,
                AfternoonStatus = afternoonStatus,
                OverallStatus = overall
            };
        }
    }
}
