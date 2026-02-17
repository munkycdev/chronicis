using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public class AzureOpenAIHealthCheckService : HealthCheckServiceBase
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public AzureOpenAIHealthCheckService(
        IConfiguration configuration, 
        HttpClient httpClient, 
        ILogger<AzureOpenAIHealthCheckService> logger)
        : base(logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    protected override Task<(string Status, string? Message)> PerformHealthCheckAsync()
    {
        var endpoint = _configuration["AzureOpenAI:Endpoint"];
        var apiKey = _configuration["AzureOpenAI:ApiKey"];
        var deploymentName = _configuration["AzureOpenAI:DeploymentName"];

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(deploymentName))
        {
            return Task.FromResult<(string Status, string? Message)>((HealthStatus.Unhealthy, "Azure OpenAI configuration missing"));
        }

        // For a lightweight check, we'll just verify the configuration is complete
        // A full check would require making an actual API call, but that might be expensive
        try
        {
            var uri = new Uri(endpoint);
            return Task.FromResult<(string Status, string? Message)>((HealthStatus.Healthy, "Azure OpenAI configuration present"));
        }
        catch (UriFormatException)
        {
            return Task.FromResult<(string Status, string? Message)>((HealthStatus.Unhealthy, "Azure OpenAI endpoint URL invalid"));
        }
    }
}
