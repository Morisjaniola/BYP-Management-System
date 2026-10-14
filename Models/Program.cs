using System.ComponentModel.DataAnnotations;

namespace BYPManagementSystem.Models
{
    public class Program
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Program Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [StringLength(250)]
        public string Schedule { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}