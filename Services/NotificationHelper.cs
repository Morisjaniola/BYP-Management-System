using BYPManagementSystem.Models;

namespace BYPManagementSystem.Services
{
    // ============================================================
    // NOTIFICATION HELPER
    // ============================================================
    //

    public static class NotificationHelper
    {
        // ==========================================================
        // BUILD A REGISTRATION-RELATED NOTIFICATION
        // ==========================================================
        //
        // Resolves the recipient Identity user from the
        // registration (UserId, falling back to the linked
        // YouthParticipant). Returns null when no recipient
        // can be resolved so callers simply skip the notification.
        //

        public static YouthNotification? ForRegistration(
            ProgramRegistration registration,
            string title,
            string message,
            string type)
        {
            var userId =
                !string.IsNullOrWhiteSpace(
                        registration.UserId)
                    ? registration.UserId
                    : registration.YouthParticipant
                        ?.UserId;


            if (string.IsNullOrWhiteSpace(
                userId))
            {
                return null;
            }


            return new YouthNotification
            {
                UserId =
                    userId,

                Title =
                    title,

                Message =
                    message,

                Type =
                    type,

                RegistrationId =
                    registration.Id,

                ProgramId =
                    registration.ProgramId,

                CreatedAt =
                    DateTime.UtcNow,

                IsRead =
                    false
            };
        }
    }
}
