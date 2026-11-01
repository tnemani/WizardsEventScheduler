using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WizardsEventScheduler.Api.Data;
using WizardsEventScheduler.Api.DTOs;
using WizardsEventScheduler.Api.Models;
using WizardsEventScheduler.Api.Services;
using Xunit;

namespace WizardsEventScheduler.Tests
{
    public class EventValidationTests
    {
        private AppDbContext GetIsolatedInMemoryDbContext(string dbName)
        {
            var connectionString = $"DataSource=file:{dbName}?mode=memory&cache=shared";
            var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA foreign_keys = OFF;";
            command.ExecuteNonQuery();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task CreateEventAsync_RejectsInvalidFormatId()
        {
            using var context = GetIsolatedInMemoryDbContext("format_validation_db");
            var service = new EventService(context, NullLogger<EventService>.Instance);

            var dto = new CreateEventDto
            {
                Name = "Bad Format Event",
                GameId = "mtg",
                GameFormatId = 999, // Non-existent format ID
                StartTime = DateTime.UtcNow.AddDays(1),
                Capacity = 16,
                Location = "Hall B"
            };

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateEventAsync(dto));
        }

        [Fact]
        public async Task GenerateIcsFileAsync_ReturnsValidBytes_WhenEventExists()
        {
            using var context = GetIsolatedInMemoryDbContext("ics_db");
            
            var format = new GameFormat { Id = 1, Name = "Commander", DefaultDurationMinutes = 240 };
            var template = new GameTemplate 
            { 
                GameId = "mtg_ics", 
                Name = "Magic: The Gathering", 
                DefaultMaxCapacity = 30,
                SupportedFormats = new List<GameFormat> { format }
            };
            context.GameTemplates.Add(template);
            await context.SaveChangesAsync();

            var service = new EventService(context, NullLogger<EventService>.Instance);
            var createdEvent = await service.CreateEventAsync(new CreateEventDto
            {
                Name = "ICS Test Event",
                GameId = "mtg_ics",
                GameFormatId = 1,
                StartTime = DateTime.UtcNow.AddDays(1),
                Capacity = 10,
                Location = "Room 1"
            });

            var icsBytes = await service.GenerateIcsFileAsync(createdEvent.Id);
            
            Assert.NotNull(icsBytes);
            Assert.True(icsBytes.Length > 0);
        }
    }
}