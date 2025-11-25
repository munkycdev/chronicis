using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Chronicis.Client.Services;

public interface IQuoteService
{
    Task<Quote?> GetRandomQuoteAsync();
}

public class QuoteService : IQuoteService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(HttpClient httpClient, ILogger<QuoteService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Quote?> GetRandomQuoteAsync()
    {
        try
        {
            _logger.LogInformation("Fetching random quote from Zen Quotes API");

            // Use CORS proxy to bypass CORS restrictions
            var response = await _httpClient.GetFromJsonAsync<List<ZenQuote>>(
                "https://corsproxy.io/?https://zenquotes.io/api/random");

            if (response?.Any() ?? false)
            {
                var zenQuote = response.First();
                return new Quote
                {
                    Content = zenQuote.Quote,
                    Author = zenQuote.Author
                };
            }

            _logger.LogWarning("No quote received from Zen Quotes API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching quote from Zen Quotes API");
        }

        // Fallback quote if API fails
        return new Quote
        {
            Content = "The world is indeed full of peril, and in it there are many dark places; but still there is much that is fair, and though in all lands love is now mingled with grief, it grows perhaps the greater.",
            Author = "J.R.R. Tolkien"
        };
    }
}

// Response model for Zen Quotes API
public class ZenQuote
{
    [JsonPropertyName("q")]
    public string Quote { get; set; } = string.Empty;

    [JsonPropertyName("a")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("h")]
    public string Html { get; set; } = string.Empty;
}

// Our internal quote model
public class Quote
{
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
}