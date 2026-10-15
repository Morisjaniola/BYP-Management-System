using BYPManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BYPManagementSystem.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =========================================================
        // YOUTH PARTICIPANTS
        // =========================================================

        public DbSet<YouthParticipant> YouthParticipants
        {
            get;
            set;
        }

        // =========================================================
        // YOUTH PROGRAMS
        // =========================================================

        public DbSet<YouthProgram> YouthPrograms
        {
            get;
            set;
        }

        // =========================================================
        // ATTENDANCE ANNOUNCEMENT LOGS
        // Idempotency markers for the scheduled attendance
        // announcements (one per program, date, and kind).
        // =========================================================

        public DbSet<AttendanceAnnouncementLog> AttendanceAnnouncementLogs
        {
            get;
            set;
        }

        // =========================================================
        // PROGRAM REGISTRATIONS
        // =========================================================

        public DbSet<ProgramRegistration> ProgramRegistrations
        {
            get;
            set;
        }

        // =========================================================
        // ATTENDANCE
        // =========================================================

        public DbSet<AttendanceRecord> AttendanceRecords
        {
            get;
            set;
        }

        // =========================================================
        // ATTENDANCE PHOTO SUBMISSIONS
        // Youth-submitted proof of attendance awaiting review.
        // =========================================================

        public DbSet<AttendancePhotoSubmission> AttendancePhotoSubmissions
        {
            get;
            set;
        }

        // ==========================================================
        // YOUTH NOTIFICATIONS
        // ==========================================================

        public DbSet<YouthNotification> YouthNotifications
        {
            get;
            set;
        }

        // ==========================================================
        // CERTIFICATES
        // 
        // Issued completion certificates for youth participants.
        // ==========================================================

        public DbSet<Certificate> Certificates
        {
            get;
            set;
        }


        // ==========================================================
        // PROGRAM REGISTRATION LOOKUP INDEX (PHASE 4)
        //
        // Composite index supporting the duplicate-detection
        // queries (program + participant + user). Deliberately NOT
        // unique: legacy rows may carry a null YouthParticipantId
        // or UserId, and MySQL treats NULLs as distinct in unique
        // indexes, so a unique constraint could not be applied
        // safely against existing data. Duplicate protection is
        // provided by the application-level checks plus the
        // transactional approval gate in RegistrationController.
        //
        // A migration (dotnet ef migrations add AddProgramRegistrationLookupIndex)
        // is required to create this index in the database; runtime
        // behavior is unaffected until it is applied.
        // ==========================================================

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(
                modelBuilder
            );


            modelBuilder.Entity<ProgramRegistration>()
                .HasIndex(
                    r =>
                        new
                        {
                            r.ProgramId,
                            r.YouthParticipantId,
                            r.UserId
                        }
                )
                .HasDatabaseName(
                    "IX_ProgramRegistrations_ProgramId_YouthParticipantId_UserId"
                );


            // ----------------------------------------------------
            // ATTENDANCE WINDOWS
            // Database defaults mirror the model defaults so rows
            // that existed before this feature receive the
            // standard barangay morning schedule
            // (7:00-8:30 AM time in, 10:30-11:30 AM time out).
            // ----------------------------------------------------

            modelBuilder.Entity<YouthProgram>()
                .Property(p => p.TimeInStart)
                .HasDefaultValue(new TimeOnly(7, 0));

            modelBuilder.Entity<YouthProgram>()
                .Property(p => p.TimeInEnd)
                .HasDefaultValue(new TimeOnly(8, 30));

            modelBuilder.Entity<YouthProgram>()
                .Property(p => p.TimeOutStart)
                .HasDefaultValue(new TimeOnly(10, 30));

            modelBuilder.Entity<YouthProgram>()
                .Property(p => p.TimeOutEnd)
                .HasDefaultValue(new TimeOnly(11, 30));


            // ----------------------------------------------------
            // SUBMISSION ACTION
            // Legacy photo submissions default to "TimeIn".
            // ----------------------------------------------------

            modelBuilder.Entity<AttendancePhotoSubmission>()
                .Property(s => s.Action)
                .HasDefaultValue("TimeIn");
        }
    }
}