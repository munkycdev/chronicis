using Chronicis.Shared.DTOs;
using MudBlazor;

namespace Chronicis.Client.Pages.Admin;

public partial class Status
{
    private bool? _authorized;
    private SystemHealthStatusDto? _healthStatus;
    private bool _isLoading = true;
    private DateTime? _lastChecked;

    protected override async Task OnInitializedAsync()
    {
        _authorized = await AdminAuth.IsSysAdminAsync();

        if (_authorized == true)
        {
            await LoadHealthStatus();
        }
    }

    private async Task RefreshStatus()
    {
        await LoadHealthStatus();
    }

    private async Task LoadHealthStatus()
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            _healthStatus = await HealthStatusApi.GetSystemHealthAsync();
            _lastChecked = DateTime.Now;
        }
        catch (Exception ex)
        {
            // Log error but don't throw - we'll show the error state
            Console.WriteLine($"Failed to load health status: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private static string GetFriendlyServiceName(string serviceKey) => serviceKey switch
    {
        ServiceKeys.Api => "API",
        ServiceKeys.Database => "Database",
        ServiceKeys.AzureOpenAI => "AI",
        ServiceKeys.BlobStorage => "Storage",
        ServiceKeys.Auth0 => "Auth",
        ServiceKeys.Open5e => "External Data",
        _ => serviceKey
    };

    private static string GetServiceIcon(string serviceKey) => serviceKey switch
    {
        ServiceKeys.Api => Icons.Material.Filled.Api,
        ServiceKeys.Database => Icons.Material.Filled.Storage,
        ServiceKeys.AzureOpenAI => Icons.Material.Filled.Psychology,
        ServiceKeys.BlobStorage => Icons.Material.Filled.Cloud,
        ServiceKeys.Auth0 => Icons.Material.Filled.Security,
        ServiceKeys.Open5e => Icons.Material.Filled.Extension,
        _ => Icons.Material.Filled.Help
    };

    private static Severity GetStatusSeverity(string status) => status switch
    {
        HealthStatus.Healthy => Severity.Success,
        HealthStatus.Degraded => Severity.Warning,
        HealthStatus.Unhealthy => Severity.Error,
        _ => Severity.Info
    };

    private static Color GetStatusColor(string status) => status switch
    {
        HealthStatus.Healthy => Color.Success,
        HealthStatus.Degraded => Color.Warning,
        HealthStatus.Unhealthy => Color.Error,
        _ => Color.Info
    };

    private static Severity GetOverallSeverity(string overallStatus) => overallStatus switch
    {
        HealthStatus.Healthy => Severity.Success,
        HealthStatus.Degraded => Severity.Warning,
        HealthStatus.Unhealthy => Severity.Error,
        _ => Severity.Info
    };

    private static string GetOverallStatusIcon(string overallStatus) => overallStatus switch
    {
        HealthStatus.Healthy => Icons.Material.Filled.CheckCircle,
        HealthStatus.Degraded => Icons.Material.Filled.Warning,
        HealthStatus.Unhealthy => Icons.Material.Filled.Error,
        _ => Icons.Material.Filled.Info
    };
}
