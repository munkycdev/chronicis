using System.Net.Http.Json;

namespace Chronicis.Client.Services;

/// <summary>
/// Extension methods for HttpClient that encapsulate common API patterns.
/// These handle try/catch, logging, and response deserialization consistently.
/// 
/// Design note: These could later be moved into an ApiServiceBase class
/// if we decide to use inheritance for service classes.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// GET request that returns a single entity or null.
    /// Logs errors and returns null on failure (including 404).
    /// </summary>
    public static async Task<T?> GetEntityAsync<T>(
        this HttpClient http,
        string url,
        ILogger logger,
        string? entityDescription = null) where T : class
    {
        try
        {
            return await http.GetFromJsonAsync<T>(url);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogWarning("{Entity} not found at {Url}", entityDescription ?? typeof(T).Name, url);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching {Entity} from {Url}", entityDescription ?? typeof(T).Name, url);
            return null;
        }
    }

    /// <summary>
    /// GET request that returns a list of entities.
    /// Logs errors and returns empty list on failure.
    /// </summary>
    public static async Task<List<T>> GetListAsync<T>(
        this HttpClient http,
        string url,
        ILogger logger,
        string? entityDescription = null)
    {
        try
        {
            var result = await http.GetFromJsonAsync<List<T>>(url);
            return result ?? new List<T>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching {Entity} list from {Url}", entityDescription ?? typeof(T).Name, url);
            return new List<T>();
        }
    }

    /// <summary>
    /// POST request that creates an entity and returns it.
    /// Returns null on failure.
    /// </summary>
    public static async Task<T?> PostEntityAsync<T>(
        this HttpClient http,
        string url,
        object dto,
        ILogger logger,
        string? entityDescription = null) where T : class
    {
        try
        {
            var response = await http.PostAsJsonAsync(url, dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>();
            }

            logger.LogWarning("Failed to create {Entity}: {StatusCode}",
                entityDescription ?? typeof(T).Name, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating {Entity} at {Url}", entityDescription ?? typeof(T).Name, url);
            return null;
        }
    }

    /// <summary>
    /// PUT request that updates an entity and returns it.
    /// Returns null on failure.
    /// </summary>
    public static async Task<T?> PutEntityAsync<T>(
        this HttpClient http,
        string url,
        object dto,
        ILogger logger,
        string? entityDescription = null) where T : class
    {
        try
        {
            var response = await http.PutAsJsonAsync(url, dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>();
            }

            logger.LogWarning("Failed to update {Entity}: {StatusCode}",
                entityDescription ?? typeof(T).Name, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating {Entity} at {Url}", entityDescription ?? typeof(T).Name, url);
            return null;
        }
    }

    /// <summary>
    /// DELETE request that returns success/failure.
    /// </summary>
    public static async Task<bool> DeleteEntityAsync(
        this HttpClient http,
        string url,
        ILogger logger,
        string? entityDescription = null)
    {
        try
        {
            var response = await http.DeleteAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to delete {Entity}: {StatusCode}",
                    entityDescription ?? "entity", response.StatusCode);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting {Entity} at {Url}", entityDescription ?? "entity", url);
            return false;
        }
    }

    /// <summary>
    /// PATCH request that returns success/failure.
    /// Useful for partial updates like moving articles.
    /// </summary>
    public static async Task<bool> PatchEntityAsync(
        this HttpClient http,
        string url,
        object dto,
        ILogger logger,
        string? entityDescription = null)
    {
        try
        {
            var response = await http.PatchAsJsonAsync(url, dto);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogWarning("Failed to patch {Entity}: {StatusCode} - {Error}",
                    entityDescription ?? "entity", response.StatusCode, errorContent);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error patching {Entity} at {Url}", entityDescription ?? "entity", url);
            return false;
        }
    }

    /// <summary>
    /// PUT request that returns success/failure (no response body expected).
    /// Useful for operations like moving entities.
    /// </summary>
    public static async Task<bool> PutBoolAsync(
        this HttpClient http,
        string url,
        object dto,
        ILogger logger,
        string? entityDescription = null)
    {
        try
        {
            var response = await http.PutAsJsonAsync(url, dto);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogWarning("Failed to update {Entity}: {StatusCode} - {Error}",
                    entityDescription ?? "entity", response.StatusCode, errorContent);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating {Entity} at {Url}", entityDescription ?? "entity", url);
            return false;
        }
    }
}
