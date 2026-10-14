using System.Collections.Generic;

namespace BYPManagementSystem.Models
{
    public class ReportsDashboardViewModel
    {
        // =========================================================
        // PARTICIPANTS
        // =========================================================

        public int TotalParticipants { get; set; }

        public int ActiveParticipants { get; set; }


        // =========================================================
        // PROGRAMS
        // =========================================================

        public int TotalPrograms { get; set; }

        public int ActivePrograms { get; set; }


        // =========================================================
        // REGISTRATIONS
        // =========================================================

        public int TotalRegistrations { get; set; }

        public int PendingRegistrations { get; set; }

        public int ApprovedRegistrations { get; set; }

        public int RejectedRegistrations { get; set; }


        // =========================================================
        // ATTENDANCE
        // =========================================================

        public int TotalAttendanceRecords { get; set; }

        public int PresentCount { get; set; }

        public int AbsentCount { get; set; }

        public int LateCount { get; set; }

        public int ExcusedCount { get; set; }


        // =========================================================
        // REPORT TABLE DATA
        // =========================================================

        public List<YouthParticipant> YouthParticipants { get; set; }
            = new List<YouthParticipant>();

        public List<YouthProgram> Programs { get; set; }
            = new List<YouthProgram>();

        public List<ProgramRegistration> Registrations { get; set; }
            = new List<ProgramRegistration>();

        public List<AttendanceRecord> AttendanceRecords { get; set; }
            = new List<AttendanceRecord>();
    }
}