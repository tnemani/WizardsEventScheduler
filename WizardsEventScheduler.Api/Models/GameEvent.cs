using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WizardsEventScheduler.Api.Models
{
    public class GameEvent
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string GameId { get; set; } = string.Empty;
        public GameTemplate GameTemplate { get; set; } = null!;

        [Required]
        public int GameFormatId { get; set; }
        public GameFormat SelectedFormat { get; set; } = null!;

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public int Capacity { get; set; }

        [Required]
        [MaxLength(250)]
        public string Location { get; set; } = string.Empty;

        // Use ConcurrencyCheck instead of Timestamp for SQLite compatibility on inserts
        [ConcurrencyCheck]
        public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();

        public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    }
}