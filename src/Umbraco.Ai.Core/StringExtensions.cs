namespace Umbraco.Ai.Core;

internal static class StringExtensions
{
    /// <summary>
    /// Converts a string to camelCase from any common case format (PascalCase, kebab-case, snake_case).
    /// </summary>
    public static string ToCamelCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        var words = SplitIntoWords(str);
        if (words.Length == 0)
            return str;

        var result = new List<string>();
        for (int i = 0; i < words.Length; i++)
        {
            var word = words[i];
            if (string.IsNullOrEmpty(word))
                continue;

            if (i == 0)
            {
                // First word: lowercase
                result.Add(word.ToLowerInvariant());
            }
            else
            {
                // Subsequent words: capitalize first letter
                result.Add(char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant());
            }
        }

        return string.Join("", result);
    }

    /// <summary>
    /// Converts a string to PascalCase from any common case format (camelCase, kebab-case, snake_case).
    /// </summary>
    public static string ToPascalCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        var words = SplitIntoWords(str);
        if (words.Length == 0)
            return str;

        var result = new List<string>();
        foreach (var word in words)
        {
            if (string.IsNullOrEmpty(word))
                continue;

            // Capitalize first letter of each word
            result.Add(char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant());
        }

        return string.Join("", result);
    }

    /// <summary>
    /// Converts a string to kebab-case from any common case format (camelCase, PascalCase, snake_case).
    /// </summary>
    public static string ToKebabCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        var words = SplitIntoWords(str);
        if (words.Length == 0)
            return str;

        return string.Join("-", words.Select(w => w.ToLowerInvariant()).Where(w => !string.IsNullOrEmpty(w)));
    }

    /// <summary>
    /// Converts a string to snake_case from any common case format (camelCase, PascalCase, kebab-case).
    /// </summary>
    public static string ToSnakeCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        var words = SplitIntoWords(str);
        if (words.Length == 0)
            return str;

        return string.Join("_", words.Select(w => w.ToLowerInvariant()).Where(w => !string.IsNullOrEmpty(w)));
    }

    /// <summary>
    /// Splits a string into words, detecting boundaries from camelCase/PascalCase, kebab-case, snake_case, spaces.
    /// </summary>
    private static string[] SplitIntoWords(string str)
    {
        if (string.IsNullOrEmpty(str))
            return [];

        var words = new List<string>();
        var currentWord = new System.Text.StringBuilder();
        var hasSeenDelimiter = false;

        for (var i = 0; i < str.Length; i++)
        {
            var c = str[i];

            // Check if this is a delimiter (hyphen, underscore, or space)
            if (c is '-' or '_' or ' ')
            {
                if (currentWord.Length > 0)
                {
                    words.Add(currentWord.ToString());
                    currentWord.Clear();
                }
                hasSeenDelimiter = true;
                continue;
            }

            // Check if this is the start of a new word (uppercase letter in camelCase/PascalCase)
            if (char.IsUpper(c) && currentWord.Length > 0 && !hasSeenDelimiter)
            {
                // Handle consecutive uppercase letters (e.g., "XMLParser" -> "XML", "Parser")
                if (i + 1 < str.Length && char.IsUpper(str[i + 1]))
                {
                    // Continue accumulating uppercase letters
                    currentWord.Append(c);
                }
                else if (i > 0 && char.IsUpper(str[i - 1]))
                {
                    // This is the last uppercase in a sequence before a lowercase
                    // Split before this character (e.g., "XML|Parser")
                    var lastChar = currentWord[^1];
                    currentWord.Length--;
                    if (currentWord.Length > 0)
                    {
                        words.Add(currentWord.ToString());
                        currentWord.Clear();
                    }
                    currentWord.Append(lastChar);
                    currentWord.Append(c);
                }
                else
                {
                    // Normal case: uppercase letter starts a new word
                    words.Add(currentWord.ToString());
                    currentWord.Clear();
                    currentWord.Append(c);
                }
            }
            else
            {
                currentWord.Append(c);
            }
        }

        // Add the last word
        if (currentWord.Length > 0)
        {
            words.Add(currentWord.ToString());
        }

        return words.ToArray();
    }
}
