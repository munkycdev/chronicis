using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Chronicis.Api.Data
{
    /// <summary>
    /// Design-time factory for creating ChronicisDbContext during migrations.
    /// This ensures EF Core tools can create the context without running the full application.
    /// </summary>
    public class ChronicisDbContextFactory : IDesignTimeDbContextFactory<ChronicisDbContext>
    {
        public ChronicisDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ChronicisDbContext>();

            // Use LocalDB connection string for migrations
            optionsBuilder.UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=Chronicis;Trusted_Connection=True;MultipleActiveResultSets=true"
            );

            return new ChronicisDbContext(optionsBuilder.Options);
        }
    }
}