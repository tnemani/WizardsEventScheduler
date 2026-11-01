using Microsoft.AspNetCore.Mvc;
using WizardsEventScheduler.Api.DTOs;
using WizardsEventScheduler.Api.Services;
using WizardsEventScheduler.Api.Models;

namespace WizardsEventScheduler.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUpcomingEvents()
        {
            var events = await _eventService.GetUpcomingEventsAsync();
            return Ok(events);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdEvent = await _eventService.CreateEventAsync(dto);
                return CreatedAtAction(nameof(GetUpcomingEvents), new { id = createdEvent.Id }, createdEvent);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{eventId}/register")]
        public async Task<IActionResult> RegisterForEvent(Guid eventId, [FromBody] RegisterPlayerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _eventService.RegisterPlayerAsync(eventId, dto);

            if (!result.IsSuccess)
            {
                // Return 409 Conflict if fully booked due to concurrency, or 400 for standard validation failures
                if (result.ErrorMessage.Contains("fully booked") || result.ErrorMessage.Contains("concurrently"))
                    return Conflict(new { message = result.ErrorMessage });

                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new { message = "Registration successful." });
        }
        /// <summary>
        /// Downloads an RFC 5545 compliant .ics calendar invite for the specified event.
        /// </summary>
        [HttpGet("{eventId}/ics")]
        public async Task<IActionResult> DownloadCalendarInvite(Guid eventId)
        {
            var icsBytes = await _eventService.GenerateIcsFileAsync(eventId);
            if (icsBytes == null)
                return NotFound(new { message = "Event not found." });

            // Return with standard MIME type text/calendar for seamless calendar client import
            return File(icsBytes, "text/calendar", $"event-{eventId}.ics");
        }
   
        /// <summary>
        /// Generates a PNG QR code encoding the event's registration link.
        /// </summary>
        [HttpGet("{eventId}/qrcode")]
        public async Task<IActionResult> GetEventQrCode(Guid eventId)
        {
            // Dynamically determine the base URL from the current request
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            
            var qrBytes = await _eventService.GenerateEventQrCodeAsync(eventId, baseUrl);
            if (qrBytes == null)
                return NotFound(new { message = "Event not found." });

            return File(qrBytes, "image/png");
        }

        [HttpGet("templates")]
        [Route("templates")]
        public async Task<ActionResult<IEnumerable<GameTemplate>>> GetTemplates()
        {
            var templates = await _eventService.GetGameTemplatesAsync();
            return Ok(templates);
        }
    }
}