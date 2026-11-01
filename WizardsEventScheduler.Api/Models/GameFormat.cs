using System.ComponentModel.DataAnnotations;

namespace WizardsEventScheduler.Api.Models
{
    public class GameFormat
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string GameTemplateId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., "Commander"

        [Range(30, 600)]
        public int DefaultDurationMinutes { get; set; } = 180;
        
        public GameTemplate GameTemplate { get; set; } = null!;
    }
}