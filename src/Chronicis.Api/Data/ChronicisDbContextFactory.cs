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

            optionsBuilder.UseSqlServer(
                "Server=localhost,1433;Database=Chronicis;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
            );

            return new ChronicisDbContext(optionsBuilder.Options);
        }
    }
}