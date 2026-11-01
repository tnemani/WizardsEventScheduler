using System;
using System.ComponentModel.DataAnnotations;

namespace WizardsEventScheduler.Api.DTOs
{
    public class CreateEventDto
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string GameId { get; set; } = string.Empty; // e.g., "mtg"

        [Required]
        public int GameFormatId { get; set; }              // e.g., Selected format ID

        [Required]
        public DateTime StartTime { get; set; }

        [Range(1, 30, ErrorMessage = "Capacity must be between 1 and 30 players.")]
        public int Capacity { get; set; }

        [Required]
        [MaxLength(250)]
        public string Location { get; set; } = string.Empty;
    }
}