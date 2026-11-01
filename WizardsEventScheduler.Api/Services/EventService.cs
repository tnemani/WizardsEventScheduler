using Microsoft.EntityFrameworkCore;
using WizardsEventScheduler.Api.Data;
using WizardsEventScheduler.Api.DTOs;
using WizardsEventScheduler.Api.Models;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using System.Text;
using QRCoder;

namespace WizardsEventScheduler.Api.Services
{
    public class EventService : IEventService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EventService> _logger;

        public EventService(AppDbContext context, ILogger<EventService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<GameEvent> CreateEventAsync(CreateEventDto dto)
        {
            // Validate that the Game Template exists
            var template = await _context.GameTemplates
                .Include(t => t.SupportedFormats)
                .FirstOrDefaultAsync(t => t.GameId == dto.GameId);

            if (template == null)
                throw new ArgumentException($"Game template '{dto.GameId}' not found.");

            // Validate that the format belongs to this template
            var format = template.SupportedFormats.FirstOrDefault(f => f.Id == dto.GameFormatId);
            if (format == null)
                throw new ArgumentException($"Selected format ID {dto.GameFormatId} is not valid for {template.Name}.");

            var gameEvent = new GameEvent
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                GameId = dto.GameId,
                GameFormatId = dto.GameFormatId,
                StartTime = dto.StartTime,
                Capacity = dto.Capacity > 0 ? dto.Capacity : template.DefaultMaxCapacity,
                Location = dto.Location,
                RowVersion = Guid.NewGuid().ToByteArray()
            };

            _context.GameEvents.Add(gameEvent);
            await _context.SaveChangesAsync();

            return gameEvent;
        }

        public async Task<IEnumerable<GameEvent>> GetUpcomingEventsAsync()
        {
            return await _context.GameEvents
                .Include(e => e.GameTemplate)
                .Include(e => e.SelectedFormat)
                .Include(e => e.Registrations)
                .Where(e => e.StartTime >= DateTime.UtcNow)
                .OrderBy(e => e.StartTime)
                .ToListAsync();
        }

        public async Task<RegistrationResult> RegisterPlayerAsync(Guid eventId, RegisterPlayerDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var gameEvent = await _context.GameEvents
                    .Include(e => e.Registrations)
                    .FirstOrDefaultAsync(e => e.Id == eventId);

                if (gameEvent == null)
                    return RegistrationResult.Fail("Event not found.");

                if (gameEvent.Registrations.Count >= gameEvent.Capacity)
                {
                    await transaction.RollbackAsync();
                    return RegistrationResult.Fail("Event is fully booked.");
                }

                if (gameEvent.Registrations.Any(r => r.PlayerEmail.Equals(dto.PlayerEmail, StringComparison.OrdinalIgnoreCase)))
                {
                    await transaction.RollbackAsync();
                    return RegistrationResult.Fail("Player is already registered for this event.");
                }

                var registration = new Registration
                {
                    Id = Guid.NewGuid(),
                    GameEventId = eventId,
                    PlayerName = dto.PlayerName,
                    PlayerEmail = dto.PlayerEmail,
                    RegisteredAt = DateTime.UtcNow
                };

                _context.Registrations.Add(registration);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                return RegistrationResult.Ok("Registration successful.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Concurrency conflict during registration for event {EventId}", eventId);
                return RegistrationResult.Fail("The event reached full capacity concurrently.");
            }
        }

        public async Task<byte[]?> GenerateIcsFileAsync(Guid eventId)
        {
            var gameEvent = await _context.GameEvents
                .Include(e => e.GameTemplate)
                .Include(e => e.SelectedFormat)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (gameEvent == null || gameEvent.GameTemplate == null || gameEvent.SelectedFormat == null) 
                return null;

            int durationMinutes = gameEvent.SelectedFormat.DefaultDurationMinutes > 0 
                ? gameEvent.SelectedFormat.DefaultDurationMinutes 
                : 180;

            var startTimeUtc = new CalDateTime(gameEvent.StartTime);
            var endTimeUtc = new CalDateTime(gameEvent.StartTime.AddMinutes(durationMinutes));

            var calendar = new Calendar();
            var calendarEvent = new CalendarEvent
            {
                Summary = $"{gameEvent.Name} ({gameEvent.GameTemplate.Name} - {gameEvent.SelectedFormat.Name})",
                Description = $"Tabletop game event organized for {gameEvent.GameTemplate.Name}. Format: {gameEvent.SelectedFormat.Name}. Capacity: {gameEvent.Capacity} players.",
                Location = gameEvent.Location,
                Start = startTimeUtc,
                End = endTimeUtc,
                Uid = $"{gameEvent.Id}@wizards-scheduler.local",
                DtStamp = new CalDateTime(DateTime.UtcNow) // Fixed property name from Stamp to DtStamp
            };

            calendar.Events.Add(calendarEvent);

            var serializer = new CalendarSerializer();
            var serializedCalendar = serializer.SerializeToString(calendar);

            return string.IsNullOrEmpty(serializedCalendar) ? null : Encoding.UTF8.GetBytes(serializedCalendar);
        }
        
        public async Task<byte[]?> GenerateEventQrCodeAsync(Guid eventId, string baseUrl)
        {
            var gameEvent = await _context.GameEvents.FindAsync(eventId);
            if (gameEvent == null) return null;

            // Construct the target registration URL for this event
            string registrationUrl = $"{baseUrl.TrimEnd('/')}/events/{eventId}/register";

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(registrationUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            
            // Generate a PNG byte array with pixels per module = 20
            return qrCode.GetGraphic(20);
        }
        public async Task<IEnumerable<GameTemplate>> GetGameTemplatesAsync()
        {
            return await _context.GameTemplates
                .Include(t => t.SupportedFormats)
                .ToListAsync();
        }
    }
}