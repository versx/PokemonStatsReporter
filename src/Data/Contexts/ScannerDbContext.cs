namespace PokemonStatsReporter.Data.Contexts
{
    using Microsoft.EntityFrameworkCore;

    using PokemonStatsReporter.Data.Entities;

    public class MapDbContext : DbContext
    {
        public MapDbContext(DbContextOptions<MapDbContext> options)
            : base(options)
        {
        }

        public DbSet<Pokemon> Pokemon { get; set; }

        public DbSet<PokemonStatsIV> PokemonStatsIV { get; set; }

        public DbSet<PokemonStatsShiny> PokemonStatsShiny { get; set; }

        public DbSet<PokemonStatsHundo> PokemonStatsHundo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PokemonStatsIV>()
                        .HasKey(p => new { p.Date, p.PokemonId });

            modelBuilder.Entity<PokemonStatsShiny>()
                        .HasKey(p => new { p.Date, p.PokemonId });

            modelBuilder.Entity<PokemonStatsHundo>()
                        .HasKey(p => new { p.Date, p.PokemonId });

            base.OnModelCreating(modelBuilder);
        }
    }
}