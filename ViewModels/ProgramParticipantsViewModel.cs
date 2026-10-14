using BYPManagementSystem.Models;

namespace BYPManagementSystem.ViewModels
{
    public class ProgramParticipantsViewModel
    {
        // ==========================================================
        // PROGRAM
        // ==========================================================

        public YouthProgram Program { get; set; } = new YouthProgram();


        // ==========================================================
        // ALL ACTIVE YOUTH PARTICIPANTS
        // ==========================================================

        public List<YouthParticipant> Participants { get; set; }
            = new List<YouthParticipant>();


        // ==========================================================
        // CURRENTLY SELECTED / ASSIGNED PARTICIPANTS
        // ==========================================================

        public HashSet<int> SelectedParticipantIds { get; set; }
            = new HashSet<int>();


        // ==========================================================
        // STATISTICS
        // ==========================================================

        public int ProgramCapacity { get; set; }

        public int SelectedCount { get; set; }

        public int AvailableYouth { get; set; }
    }
}