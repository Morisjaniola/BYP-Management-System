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
        }
    }
}