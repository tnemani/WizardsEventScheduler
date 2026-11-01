using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WizardsEventScheduler.Api.Models;

namespace WizardsEventScheduler.Api.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // Ensure the SQLite database file and tables are created
            await context.Database.EnsureCreatedAsync();

            // Bind configuration section to template DTOs
            var configuredTemplates = configuration.GetSection("GameTemplates").Get<List<GameTemplateConfigDto>>();
            if (configuredTemplates == null || !configuredTemplates.Any()) return;

            int globalFormatId = 1;

            foreach (var configTemplate in configuredTemplates)
            {
                var existingTemplate = await context.GameTemplates
                    .Include(t => t.SupportedFormats)
                    .FirstOrDefaultAsync(t => t.GameId == configTemplate.GameId);

                if (existingTemplate == null)
                {
                    // Dynamically hydrate new template and assign unique sequential format IDs
                    var newTemplate = new GameTemplate
                    {
                        GameId = configTemplate.GameId,
                        Name = configTemplate.Name,
                        DefaultMaxCapacity = configTemplate.DefaultMaxCapacity,
                        MinPlayersToStart = configTemplate.MinPlayersToStart,
                        SupportedFormats = configTemplate.Formats.Select(f => new GameFormat
                        {
                            Id = globalFormatId++, // Sequentially assigns unique IDs (1, 2, 3, 4...) across all templates
                            Name = f.Name,
                            DefaultDurationMinutes = f.DefaultDurationMinutes,
                            GameTemplateId = configTemplate.GameId
                        }).ToList()
                    };

                    context.GameTemplates.Add(newTemplate);
                }
                else
                {
                    // Keep existing template synced with configuration updates
                    existingTemplate.Name = configTemplate.Name;
                    existingTemplate.DefaultMaxCapacity = configTemplate.DefaultMaxCapacity;
                    existingTemplate.MinPlayersToStart = configTemplate.MinPlayersToStart;
                }
            }

            await context.SaveChangesAsync();
        }
    }
}