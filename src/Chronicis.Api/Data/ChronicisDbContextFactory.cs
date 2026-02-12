using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Chronicis.Api.Data
{
    /// <summary>
    /// Design-time factory for creating ChronicisDbContext during migrations.
    /// This ensures EF Core tools can create the context without running the full application.
    /// 
    /// Connection string resolution (in order of priority):
    /// 1. Environment variable: CHRONICIS_CONNECTION_STRING
    /// 2. local.settings.json: ConnectionStrings:ChronicisDb
    /// 3. Fallback to localhost SQL Server (for local development)
    /// </summary>
    public class ChronicisDbContextFactory : IDesignTimeDbContextFactory<ChronicisDbContext>
    {
        public ChronicisDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ChronicisDbContext>();

            // Try to get connection string from environment variable first
            var connectionString = Environment.GetEnvironmentVariable("CHRONICIS_CONNECTION_STRING");

            // If not found, try to load from local.settings.json
            if (string.IsNullOrEmpty(connectionString))
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: false)
                    .Build();

                connectionString = configuration.GetConnectionString("ChronicisDb");
            }

            // Fallback for local development (no secrets in source code)
            if (string.IsNullOrEmpty(connectionString))
            {
                // This fallback assumes local SQL Server with integrated security
                // Developers should set up local.settings.json or environment variable
                connectionString = "Server=localhost,1433;Database=Chronicis;Integrated Security=True;TrustServerCertificate=True";
                Console.WriteLine("WARNING: Using fallback connection string. Set CHRONICIS_CONNECTION_STRING or configure local.settings.json");
            }

            optionsBuilder.UseSqlServer(connectionString);

            return new ChronicisDbContext(optionsBuilder.Options);
        }
    }
}
