using System.Security.Cryptography;

namespace Chronicis.Api.Utilities;

/// <summary>
/// Generates memorable invitation codes in XXXX-XXXX format.
/// Uses word-like patterns that are easy to read aloud in Discord.
/// </summary>
public static class InvitationCodeGenerator
{
    // Consonants and vowels for pronounceable codes
    private static readonly char[] Consonants = "BCDFGHJKLMNPRSTVWXZ".ToCharArray();
    private static readonly char[] Vowels = "AEIOU".ToCharArray();

    /// <summary>
    /// Generate a memorable code like "FROG-AXLE" or "MINT-RUBY"
    /// Pattern: CVCC-VCCV or similar pronounceable combinations
    /// </summary>
    public static string GenerateCode()
    {
        var part1 = GenerateWordLikePart();
        var part2 = GenerateWordLikePart();
        return $"{part1}-{part2}";
    }

    private static string GenerateWordLikePart()
    {
        // Generate a 4-character pronounceable part
        // Pattern alternates to create word-like strings
        var chars = new char[4];
        
        // Random pattern: start with consonant or vowel
        bool startWithConsonant = RandomNumberGenerator.GetInt32(2) == 0;
        
        for (int i = 0; i < 4; i++)
        {
            bool useConsonant = (i % 2 == 0) ? startWithConsonant : !startWithConsonant;
            
            if (useConsonant)
            {
                chars[i] = Consonants[RandomNumberGenerator.GetInt32(Consonants.Length)];
            }
            else
            {
                chars[i] = Vowels[RandomNumberGenerator.GetInt32(Vowels.Length)];
            }
        }
        
        return new string(chars);
    }

    /// <summary>
    /// Normalize a code for comparison (uppercase, with hyphen)
    /// </summary>
    public static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        // Remove spaces, uppercase
        var cleaned = code.Trim().ToUpperInvariant().Replace(" ", "");
        
        // If no hyphen and 8 chars, insert hyphen
        if (!cleaned.Contains('-') && cleaned.Length == 8)
        {
            cleaned = $"{cleaned[..4]}-{cleaned[4..]}";
        }

        return cleaned;
    }

    /// <summary>
    /// Validate code format (XXXX-XXXX)
    /// </summary>
    public static bool IsValidFormat(string code)
    {
        var normalized = NormalizeCode(code);
        
        if (normalized.Length != 9) // XXXX-XXXX
            return false;

        if (normalized[4] != '-')
            return false;

        // Check all other chars are letters
        for (int i = 0; i < 9; i++)
        {
            if (i == 4) continue;
            if (!char.IsLetter(normalized[i]))
                return false;
        }

        return true;
    }
}
