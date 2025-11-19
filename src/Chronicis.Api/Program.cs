using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Database configuration
        var connectionString = context.Configuration.GetConnectionString("ChronicisDb")
            ?? throw new InvalidOperationException("Connection string 'ChronicisDb' not found.");

        services.AddDbContext<ChronicisDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register services
        services.AddScoped<IArticleService, ArticleService>();
    })
    .Build();

host.Run();