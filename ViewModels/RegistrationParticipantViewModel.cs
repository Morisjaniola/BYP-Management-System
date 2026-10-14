using BYPManagementSystem.Models;

namespace BYPManagementSystem.ViewModels
{
    public class RegistrationParticipantViewModel
    {
        // ==========================================================
        // YOUTH PARTICIPANT
        // ==========================================================

        public YouthParticipant YouthParticipant { get; set; }
            = new YouthParticipant();


        // ==========================================================
        // USER / ACCOUNT
        // ==========================================================

        public string? UserEmail { get; set; }

        public bool HasUserAccount { get; set; }


        // ==========================================================
        // REGISTERED / ASSIGNED PROGRAMS
        // ==========================================================

        public List<ProgramRegistration> Registrations { get; set; }
            = new List<ProgramRegistration>();


        // ==========================================================
        // SUMMARY
        // ==========================================================

        public int TotalPrograms =>
            Registrations.Count;

        public int PendingPrograms =>
            Registrations.Count(r =>
                r.Status == "Pending");

        public int ApprovedPrograms =>
            Registrations.Count(r =>
                r.Status == "Approved");

        public int RejectedPrograms =>
            Registrations.Count(r =>
                r.Status == "Rejected");
    }
}