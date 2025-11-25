namespace Chronicis.Api.Services;

/// <summary>
/// Service for parsing and extracting hashtags from article content
/// </summary>
public interface IHashtagParser
{
    /// <summary>
    /// Extracts all hashtags from the given text
    /// </summary>
    /// <param name="text">Text to parse (article body)</param>
    /// <returns>List of hashtag information including name and position</returns>
    List<HashtagMatch> ExtractHashtags(string text);
}

/// <summary>
/// Represents a hashtag found in text
/// </summary>
public class HashtagMatch
{
    /// <summary>
    /// The hashtag name (without the # symbol, lowercase)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Character position in the text where this hashtag appears
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// The full matched text including # symbol
    /// </summary>
    public string FullMatch { get; set; } = string.Empty;
}
