using BYPManagementSystem.Data;
using BYPManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BYPManagementSystem.Services
{
    // ============================================================
    // ATTENDANCE ANNOUNCEMENT SERVICE
    // ============================================================
    //
    // Server-side scheduler that announces the configured
    // attendance windows to approved participants of active
    // programs. Runs inside the web host, so it works even when
    // no Admin browser is open.
    //
    // For every active program the service tracks four triggers
    // per attendance day (Philippine time):
    //
    //   TimeInOpen  at TimeInStart          "Morning Time In is now open."
    //   TimeInSoon  15 min before TimeInEnd "Morning Time In will close soon."
    //   TimeOutOpen at TimeOutStart         "Morning Time Out is now open."
    //   TimeOutSoon 15 min before TimeOutEnd"Morning Time Out will close soon."
    //
    // A trigger fires when the current Manila time has reached
    // it and is still within a 30 minute grace window (which
    // also covers server restarts), and only when no log row
    // exists yet for that program + date + kind. The unique
    // index on the log table guarantees each announcement is
    // sent at most once, so refreshes or repeated ticks never
    // duplicate notifications.
    //

    public class AttendanceAnnouncementService
        : BackgroundService
    {
        private static readonly TimeSpan TickInterval =
            TimeSpan.FromSeconds(30);

        private static readonly TimeSpan GraceWindow =
            TimeSpan.FromMinutes(30);

        private const int ClosingSoonMinutes = 15;


        private readonly IServiceScopeFactory _scopeFactory;

        private readonly ILogger<AttendanceAnnouncementService> _logger;


        public AttendanceAnnouncementService(
            IServiceScopeFactory scopeFactory,
            ILogger<AttendanceAnnouncementService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }


        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            // Small startup delay so the application finishes
            // booting (migrations, seeding) before the first tick.

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(10),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }


            using var timer =
                new PeriodicTimer(TickInterval);

            try
            {
                while (await timer.WaitForNextTickAsync(
                           stoppingToken))
                {
                    try
                    {
                        await RunOnceAsync(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // A failed tick must never stop the
                        // scheduler; it retries on the next tick.

                        _logger.LogError(
                            ex,
                            "Attendance announcement tick failed.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
        }


        // ==========================================================
        // ONE SCHEDULER PASS
        // ==========================================================

        public async Task RunOnceAsync(
            CancellationToken cancellationToken)
        {
            var manilaNow =
                AttendanceSchedule.NowManila();

            var attendanceDate =
                manilaNow.Date;

            var nowTime =
                TimeOnly.FromDateTime(manilaNow);


            using var scope =
                _scopeFactory.CreateScope();

            var db =
                scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();


            var programs =
                await db.YouthPrograms
                    .AsNoTracking()
                    .Where(p => p.Status == "Active")
                    .ToListAsync(cancellationToken);


            foreach (var program in programs)
            {
                foreach (var trigger in BuildTriggers(program))
                {
                    if (nowTime < trigger.At)
                    {
                        // Not due yet today.
                        continue;
                    }


                    var age =
                        manilaNow
                        - attendanceDate.Add(
                            trigger.At.ToTimeSpan());

                    if (age > GraceWindow)
                    {
                        // Due, but the grace window passed long
                        // ago (for example the server was down).
                        // Never send stale announcements.
                        continue;
                    }


                    await TryAnnounceAsync(
                        db,
                        program,
                        attendanceDate,
                        trigger,
                        cancellationToken);
                }
            }
        }


        // ==========================================================
        // TRIGGERS FOR ONE PROGRAM
        // ==========================================================

        private static List<AnnouncementTrigger> BuildTriggers(
            YouthProgram program)
        {
            var timeInSoon =
                ClampAfter(
                    program.TimeInEnd.AddMinutes(
                        -ClosingSoonMinutes),
                    program.TimeInStart);

            var timeOutSoon =
                ClampAfter(
                    program.TimeOutEnd.AddMinutes(
                        -ClosingSoonMinutes),
                    program.TimeOutStart);


            return new List<AnnouncementTrigger>
            {
                new AnnouncementTrigger(
                    "TimeInOpen",
                    program.TimeInStart,
                    "Morning Time In Open",
                    "Morning Time In is now open. You may submit your Time In photo for \""
                        + program.Name
                        + "\" until "
                        + AttendanceSchedule.FormatTime(program.TimeInEnd)
                        + " (Philippine time)."),

                new AnnouncementTrigger(
                    "TimeInSoon",
                    timeInSoon,
                    "Morning Time In Closing Soon",
                    "Morning Time In will close soon. The Time In window for \""
                        + program.Name
                        + "\" ends at "
                        + AttendanceSchedule.FormatTime(program.TimeInEnd)
                        + " (Philippine time)."),

                new AnnouncementTrigger(
                    "TimeOutOpen",
                    program.TimeOutStart,
                    "Morning Time Out Open",
                    "Morning Time Out is now open. You may submit your Time Out photo for \""
                        + program.Name
                        + "\" until "
                        + AttendanceSchedule.FormatTime(program.TimeOutEnd)
                        + " (Philippine time)."),

                new AnnouncementTrigger(
                    "TimeOutSoon",
                    timeOutSoon,
                    "Morning Time Out Closing Soon",
                    "Morning Time Out will close soon. The Time Out window for \""
                        + program.Name
                        + "\" ends at "
                        + AttendanceSchedule.FormatTime(program.TimeOutEnd)
                        + " (Philippine time).")
            };
        }


        private static TimeOnly ClampAfter(
            TimeOnly value,
            TimeOnly minimum) =>
            value < minimum ? minimum : value;


        // ==========================================================
        // SEND ONE ANNOUNCEMENT (IDEMPOTENT)
        // ==========================================================

        private async Task TryAnnounceAsync(
            ApplicationDbContext db,
            YouthProgram program,
            DateTime attendanceDate,
            AnnouncementTrigger trigger,
            CancellationToken cancellationToken)
        {
            var alreadySent =
                await db.AttendanceAnnouncementLogs
                    .AnyAsync(
                        l =>
                            l.ProgramId == program.Id &&
                            l.AttendanceDate ==
                                attendanceDate &&
                            l.Kind == trigger.Kind,
                        cancellationToken);


            if (alreadySent)
            {
                return;
            }


            // ------------------------------------------------------
            // RECIPIENTS
            // Approved registrations with an active participant
            // (or a linked Identity user for legacy rows).
            // ------------------------------------------------------

            var registrations =
                await db.ProgramRegistrations
                    .AsNoTracking()
                    .Include(r => r.YouthParticipant)
                    .Where(r =>
                        r.ProgramId == program.Id &&
                        r.Status == "Approved")
                    .ToListAsync(cancellationToken);


            var eligible =
                registrations
                    .Where(r =>
                        r.YouthParticipant != null
                            ? r.YouthParticipant.IsActive
                            : !string.IsNullOrWhiteSpace(
                                r.UserId))
                    .ToList();


            if (!eligible.Any())
            {
                // Still log the trigger so a later participant
                // addition cannot retro-announce an old window.
                // Nothing to send means nothing to store.
                return;
            }


            var log =
                new AttendanceAnnouncementLog
                {
                    ProgramId = program.Id,

                    AttendanceDate = attendanceDate,

                    Kind = trigger.Kind,

                    SentAt = DateTime.UtcNow
                };

            db.AttendanceAnnouncementLogs.Add(log);


            var sent = 0;

            foreach (var registration in eligible)
            {
                var notification =
                    NotificationHelper.ForRegistration(
                        registration,
                        trigger.Title,
                        trigger.Message,
                        "Announcement");

                if (notification == null)
                {
                    continue;
                }

                db.YouthNotifications.Add(notification);
                sent++;
            }


            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // A concurrent pass already logged this exact
                // trigger; the unique index rejected ours, so
                // the whole save (log + notifications) rolled
                // back. Nothing was duplicated.

                db.ChangeTracker.Clear();

                return;
            }

            _logger.LogInformation(
                "Attendance announcement {Kind} sent for program {ProgramId} on {Date} to {Count} participant(s).",
                trigger.Kind,
                program.Id,
                attendanceDate,
                sent);
        }


        // ==========================================================
        // TRIGGER VALUE TYPE
        // ==========================================================

        private sealed record AnnouncementTrigger(
            string Kind,
            TimeOnly At,
            string Title,
            string Message);
    }
}
