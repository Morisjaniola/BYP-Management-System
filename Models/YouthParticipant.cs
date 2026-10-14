using System.ComponentModel.DataAnnotations;

namespace BYPManagementSystem.Models
{
    public class YouthParticipant
    {
        public int Id { get; set; }

        // ==========================================================
        // NAME
        // ==========================================================

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;


        // ==========================================================
        // BIRTH DATE
        // ==========================================================

        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }


        // ==========================================================
        // CONTACT
        // ==========================================================

        [StringLength(30)]
        public string? ContactNumber { get; set; }


        // ==========================================================
        // ADDRESS
        // ==========================================================

        [StringLength(250)]
        public string? Address { get; set; }


        // ==========================================================
        // BARANGAY
        //
        // Used for automatic program assignment.
        // ==========================================================

        [Required]
        [StringLength(100)]
        public string Barangay { get; set; } = string.Empty;


        // ==========================================================
        // IDENTITY USER
        // ==========================================================

        [StringLength(450)]
        public string? UserId { get; set; }


        // ==========================================================
        // ACTIVE STATUS
        // ==========================================================

        public bool IsActive { get; set; } = true;


        // ==========================================================
        // CREATED DATE
        // ==========================================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // ==========================================================
        // FULL NAME
        // ==========================================================

        public string FullName =>
            $"{FirstName} {LastName}";
    }
}