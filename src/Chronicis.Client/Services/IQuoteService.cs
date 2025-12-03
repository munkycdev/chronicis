
namespace Chronicis.Client.Services;

public interface IQuoteService
{
    Task<Quote?> GetRandomQuoteAsync();
}