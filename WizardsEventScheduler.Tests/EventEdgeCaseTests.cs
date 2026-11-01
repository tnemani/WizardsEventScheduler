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
    public class EventEdgeCaseTests
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
        public async Task CreateEventAsync_RejectsInvalidCapacity()
        {
            using var context = GetIsolatedInMemoryDbContext("edge_case_db");
            var service = new EventService(context, NullLogger<EventService>.Instance);

            var dto = new CreateEventDto
            {
                Name = "Invalid Event",
                GameId = "mtg",
                GameFormatId = 1,
                StartTime = DateTime.UtcNow.AddDays(1),
                Capacity = 500, // Exceeds default max capacity or bounds
                Location = "Hall A"
            };

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateEventAsync(dto));
        }

        [Fact]
        public async Task RegisterPlayerAsync_Fails_WhenEventNotFound()
        {
            using var context = GetIsolatedInMemoryDbContext("not_found_db");
            var service = new EventService(context, NullLogger<EventService>.Instance);

            var result = await service.RegisterPlayerAsync(Guid.NewGuid(), new RegisterPlayerDto
            {
                PlayerName = "Ghost Player",
                PlayerEmail = "ghost@test.com"
            });

            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }
    }
}