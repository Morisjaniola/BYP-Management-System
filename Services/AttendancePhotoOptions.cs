namespace BYPManagementSystem.Services
{
    // ============================================================
    // ATTENDANCE PHOTO OPTIONS
    // ============================================================
    //
    // Bound from the "AttendancePhotos" configuration section.
    //
    // FolderPath is resolved against the application content root
    // (NOT wwwroot), so uploaded attendance photos are never
    // served as unrestricted static files.
    //

    public class AttendancePhotoOptions
    {
        public const string SectionName = "AttendancePhotos";


        // Relative to the content root.
        public string FolderPath { get; set; } =
            "App_Data/AttendancePhotos";


        public long MaxFileSizeBytes { get; set; } =
            5 * 1024 * 1024;
    }
}
