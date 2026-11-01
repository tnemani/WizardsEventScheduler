using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WizardsEventScheduler.Api.Data;
using WizardsEventScheduler.Api.DTOs;
using WizardsEventScheduler.Api.Models;
using WizardsEventScheduler.Api.Services;
using Xunit;

namespace WizardsEventScheduler.Tests
{
    public class EventConcurrencyTests
    {
        private AppDbContext GetIsolatedInMemoryDbContext(string dbName)
        {
            var connectionString = $"DataSource=file:{dbName}?mode=memory&cache=shared";
            var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
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
        public async Task RegisterPlayerAsync_EnforcesCapacity_UnderConcurrentLoad()
        {
            using var setupContext = GetIsolatedInMemoryDbContext("concurrency_db");

            var format = new GameFormat { Id = 1, Name = "Commander", DefaultDurationMinutes = 180 };
            var template = new GameTemplate 
            { 
                GameId = "mtg_conc", 
                Name = "Magic: The Gathering", 
                DefaultMaxCapacity = 30,
                SupportedFormats = new List<GameFormat> { format }
            };

            setupContext.GameTemplates.Add(template);
            await setupContext.SaveChangesAsync();

            var setupService = new EventService(setupContext, NullLogger<EventService>.Instance);

            var createdEvent = await setupService.CreateEventAsync(new CreateEventDto
            {
                Name = "Draft Championship",
                GameId = "mtg_conc",
                GameFormatId = 1,
                StartTime = DateTime.UtcNow.AddDays(1),
                Capacity = 2,
                Location = "Main Hall"
            });

            var tasks = new List<Task<RegistrationResult>>();
            for (int i = 1; i <= 5; i++)
            {
                int index = i;
                tasks.Add(Task.Run(async () =>
                {
                    using var threadContext = GetIsolatedInMemoryDbContext("concurrency_db");
                    var threadService = new EventService(threadContext, NullLogger<EventService>.Instance);
                    
                    return await threadService.RegisterPlayerAsync(createdEvent.Id, new RegisterPlayerDto
                    {
                        PlayerName = $"Player {index}",
                        PlayerEmail = $"player{index}@test.com"
                    });
                }));
            }

            var results = await Task.WhenAll(tasks);
            int successfulRegistrations = results.Count(r => r.IsSuccess);

            Assert.True(successfulRegistrations <= 2, 
                $"Expected at most 2 successful registrations under concurrent load, but got {successfulRegistrations}");
        }

        [Fact]
        public async Task RegisterPlayerAsync_PreventsDuplicateEmailRegistration()
        {
            using var setupContext = GetIsolatedInMemoryDbContext("duplicate_db");

            var format = new GameFormat { Id = 1, Name = "Commander", DefaultDurationMinutes = 180 };
            var template = new GameTemplate 
            { 
                GameId = "mtg_dup", 
                Name = "Magic: The Gathering", 
                DefaultMaxCapacity = 30,
                SupportedFormats = new List<GameFormat> { format }
            };

            setupContext.GameTemplates.Add(template);
            await setupContext.SaveChangesAsync();

            var service = new EventService(setupContext, NullLogger<EventService>.Instance);

            var createdEvent = await service.CreateEventAsync(new CreateEventDto
            {
                Name = "Standard Showdown",
                GameId = "mtg_dup",
                GameFormatId = 1,
                StartTime = DateTime.UtcNow.AddDays(2),
                Capacity = 10,
                Location = "Table 3"
            });

            var dto = new RegisterPlayerDto
            {
                PlayerName = "Alice Smith",
                PlayerEmail = "alice@test.com"
            };

            var firstAttempt = await service.RegisterPlayerAsync(createdEvent.Id, dto);
            var secondAttempt = await service.RegisterPlayerAsync(createdEvent.Id, dto);

            Assert.True(firstAttempt.IsSuccess);
            Assert.False(secondAttempt.IsSuccess);
        }
    }
}