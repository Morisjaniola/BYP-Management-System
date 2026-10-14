using System.ComponentModel.DataAnnotations;

namespace BYPManagementSystem.Models
{
    public class ProgramRegistration
    {
        public int Id { get; set; }

        // ==========================================================
        // PROGRAM
        // ==========================================================

        [Required]
        public int ProgramId { get; set; }

        public YouthProgram? Program { get; set; }


        // ==========================================================
        // YOUTH PARTICIPANT
        // ==========================================================

        public int? YouthParticipantId { get; set; }

        public YouthParticipant? YouthParticipant { get; set; }


        // ==========================================================
        // IDENTITY USER
        // ==========================================================

        [StringLength(450)]
        public string? UserId { get; set; }


        // ==========================================================
        // EMAIL
        // ==========================================================

        [StringLength(256)]
        public string? UserEmail { get; set; }


        // ==========================================================
        // STATUS
        // ==========================================================

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";


        // ==========================================================
        // REGISTERED DATE
        // ==========================================================

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    }
}