using Microsoft.EntityFrameworkCore;
using WizardsEventScheduler.Api.Data;
using WizardsEventScheduler.Api.DTOs;
using WizardsEventScheduler.Api.Models;

namespace WizardsEventScheduler.Api.Services
{
    public interface IEventService
    {
        Task<GameEvent> CreateEventAsync(CreateEventDto dto);
        Task<IEnumerable<GameEvent>> GetUpcomingEventsAsync();
        Task<RegistrationResult> RegisterPlayerAsync(Guid eventId, RegisterPlayerDto dto);
        Task<byte[]?> GenerateIcsFileAsync(Guid eventId);
        Task<byte[]?> GenerateEventQrCodeAsync(Guid eventId, string baseUrl);
        Task<IEnumerable<GameTemplate>> GetGameTemplatesAsync();
    }
}