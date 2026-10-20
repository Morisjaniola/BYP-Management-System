namespace BYPManagementSystem.ViewModels
{
    // ============================================================
    // VIEW PARTICIPANTS - MAIN TABLE ROW
    //
    // One row per approved registration for the selected
    // attendance date. Each of the four attendance actions has
    // its own cell state (approved time / pending / rejected /
    // none) plus the overall computed status for the day.
    // ============================================================

    public class AttendanceActionCellViewModel
    {
        // Approved | Pending | Rejected | None

        public string Kind { get; set; } = "None";

        // Formatted app-time of the approved submission, when
        // Kind == Approved. Empty otherwise.

        public string TimeDisplay { get; set; } = "";
    }


    public class ParticipantAttendanceRowViewModel
    {
        public int RowNumber { get; set; }

        public int RegistrationId { get; set; }

        public int YouthParticipantId { get; set; }

        public string FullName { get; set; } = "Unknown Participant";

        public string? Barangay { get; set; }

        public DateTime RegisteredAt { get; set; }

        public AttendanceActionCellViewModel MorningIn { get; set; } = new();

        public AttendanceActionCellViewModel MorningOut { get; set; } = new();

        public AttendanceActionCellViewModel AfternoonIn { get; set; } = new();

        public AttendanceActionCellViewModel AfternoonOut { get; set; } = new();

        // Computed overall status text, e.g. "Pending Review".

        public string StatusLabel { get; set; } = "";

        // Badge CSS key, e.g. "st-pending".

        public string StatusCss { get; set; } = "";
    }
}
