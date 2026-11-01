using Microsoft.EntityFrameworkCore;
using WizardsEventScheduler.Api.Models;

namespace WizardsEventScheduler.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<GameTemplate> GameTemplates => Set<GameTemplate>();
        public DbSet<GameFormat> GameFormats => Set<GameFormat>();
        public DbSet<GameEvent> GameEvents => Set<GameEvent>();
        public DbSet<Registration> Registrations => Set<Registration>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // GameTemplate -> GameFormats Relationship
            modelBuilder.Entity<GameTemplate>()
                .HasMany(t => t.SupportedFormats)
                .WithOne(f => f.GameTemplate)
                .HasForeignKey(f => f.GameTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            // GameEvent -> GameTemplate Relationship
            modelBuilder.Entity<GameEvent>()
                .HasOne(e => e.GameTemplate)
                .WithMany()
                .HasForeignKey(e => e.GameId)
                .OnDelete(DeleteBehavior.Restrict);

            // GameEvent -> GameFormat Relationship
            modelBuilder.Entity<GameEvent>()
                .HasOne(e => e.SelectedFormat)
                .WithMany()
                .HasForeignKey(e => e.GameFormatId)
                .OnDelete(DeleteBehavior.Restrict);

            // GameEvent -> Registrations Relationship
            modelBuilder.Entity<Registration>()
                .HasOne(r => r.GameEvent)
                .WithMany(e => e.Registrations)
                .HasForeignKey(r => r.GameEventId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}