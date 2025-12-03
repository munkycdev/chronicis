
namespace Chronicis.Client.Services;

public interface IAuthService
{
    Task<UserInfo?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
}