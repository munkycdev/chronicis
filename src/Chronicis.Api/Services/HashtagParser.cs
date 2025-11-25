using System.Text.RegularExpressions;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for parsing and extracting hashtags from article content
/// </summary>
public class HashtagParser : IHashtagParser
{
    // Regex pattern: # followed by one or more word characters (letters, digits, underscore)
    // Does NOT match hashtags inside code blocks or links
    private static readonly Regex HashtagRegex = new(
        @"(?<!`)#(\w+)(?!`)",
        RegexOptions.Compiled | RegexOptions.Multiline
    );

    /// <summary>
    /// Extracts all hashtags from the given text
    /// </summary>
    /// <param name="text">Text to parse (article body)</param>
    /// <returns>List of hashtag information including name and position</returns>
    public List<HashtagMatch> ExtractHashtags(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<HashtagMatch>();
        }

        var matches = new List<HashtagMatch>();
        var regexMatches = HashtagRegex.Matches(text);

        foreach (Match match in regexMatches)
        {
            // Group 1 contains the hashtag name without the # symbol
            var hashtagName = match.Groups[1].Value.ToLowerInvariant();

            matches.Add(new HashtagMatch
            {
                Name = hashtagName,
                Position = match.Index,
                FullMatch = match.Value // Includes the # symbol
            });
        }

        return matches;
    }
}
