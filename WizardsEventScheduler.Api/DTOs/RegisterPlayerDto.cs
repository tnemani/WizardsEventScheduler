using System.ComponentModel.DataAnnotations;

namespace WizardsEventScheduler.Api.DTOs
{
    public class RegisterPlayerDto
    {
        [Required]
        [MaxLength(100)]
        public string PlayerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string PlayerEmail { get; set; } = string.Empty;
    }
}