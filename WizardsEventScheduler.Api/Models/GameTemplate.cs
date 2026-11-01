using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WizardsEventScheduler.Api.Models
{
    public class GameTemplate
    {
        [Key]
        [Required]
        [MaxLength(50)]
        public string GameId { get; set; } = string.Empty; // e.g., "mtg", "pokemon", "yugioh"

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., "Magic: The Gathering"

        [Range(1, 30)]
        public int DefaultMaxCapacity { get; set; } = 30;

        [Range(1, 10)]
        public int MinPlayersToStart { get; set; } = 4;
        
        public ICollection<GameFormat> SupportedFormats { get; set; } = new List<GameFormat>();
    }
}