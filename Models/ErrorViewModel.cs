namespace BYPManagementSystem.Models
{
    public class ErrorViewModel
    {
        // =========================================================
        // REQUEST ID
        // =========================================================

        public string? RequestId { get; set; }


        // =========================================================
        // SHOW REQUEST ID
        // =========================================================

        public bool ShowRequestId =>
            !string.IsNullOrEmpty(RequestId);
    }
}