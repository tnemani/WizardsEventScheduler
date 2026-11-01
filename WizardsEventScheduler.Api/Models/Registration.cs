using System;
using System.ComponentModel.DataAnnotations;

namespace WizardsEventScheduler.Api.Models
{
    public class Registration
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid GameEventId { get; set; }

        [Required]
        [MaxLength(100)]
        public string PlayerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string PlayerEmail { get; set; } = string.Empty;

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public GameEvent GameEvent { get; set; } = null!;
    }
}