using BYPManagementSystem.Models;

namespace BYPManagementSystem.ViewModels
{
    // ==========================================================
    // ADMIN YOUTH PARTICIPANT DISPLAY MODEL
    //
    // Combines the youth participant row with the LATEST values
    // from its linked Identity account (YouthParticipant.UserId
    // -> AspNetUsers.Id):
    //   - Email       : AspNetUsers.Email
    //   - Profile pic : the profile-image path stored by
    //                   ProfileController in AspNetUserClaims
    // Enrichment happens on every request, so admin pages always
    // render synchronized profile data - never stale copies -
    // and no extra database columns (or migrations) are needed.
    // ==========================================================

    public class ParticipantProfileViewModel
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public DateTime? BirthDate { get; set; }

        public string? ContactNumber { get; set; }

        public string? Address { get; set; }

        public string Barangay { get; set; } = string.Empty;

        public string? UserId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // ==========================================================
        // LINKED ACCOUNT (LATEST VALUES)
        // ==========================================================

        public string? Email { get; set; }

        public string? ProfileImagePath { get; set; }

        public bool HasProfileImage =>
            !string.IsNullOrWhiteSpace(ProfileImagePath);


        // ==========================================================
        // FULL NAME
        // ==========================================================

        public string FullName =>
            $"{FirstName} {LastName}";


        // ==========================================================
        // MAPPING
        // ==========================================================

        public static ParticipantProfileViewModel From(
            YouthParticipant participant)
        {
            return new ParticipantProfileViewModel
            {
                Id = participant.Id,
                FirstName = participant.FirstName,
                LastName = participant.LastName,
                BirthDate = participant.BirthDate,
                ContactNumber = participant.ContactNumber,
                Address = participant.Address,
                Barangay = participant.Barangay,
                UserId = participant.UserId,
                IsActive = participant.IsActive,
                CreatedAt = participant.CreatedAt
            };
        }
    }
}
