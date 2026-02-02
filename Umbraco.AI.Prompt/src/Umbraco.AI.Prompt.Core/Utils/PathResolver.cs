using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Umbraco.AI.Prompt.Core.Utils;

internal sealed class PathResolverOptions
{
    /// <summary>
    /// If true, key/property matching is case-sensitive.
    /// Default = false (case-insensitive).
    /// </summary>
    public bool CaseSensitive { get; init; } = false;
}

internal static class PathResolver
{
    /// <summary>
    /// Resolve a value from a root object using a path expression like:
    /// inner["My.Key"], inner.arr[0]['x.y'][2].z
    /// Works with POCOs, IDictionary, IDictionary&lt;string, object&gt;, arrays/lists, and System.Text.Json JsonElement.
    /// Returns null if anything along the path is missing/out-of-range.
    /// </summary>
    public static object? Resolve(object? root, string path, PathResolverOptions? options = null)
    {
        if (root is null) return null;
        if (string.IsNullOrWhiteSpace(path)) return root;

        options ??= new PathResolverOptions();

        foreach (var token in Tokenize(path))
        {
            if (root is null) return null;

            root = token.Kind switch
            {
                TokenKind.Property => GetMemberValue(root, token.Value, options),
                TokenKind.Key      => GetKeyValue(root, token.Value, options),
                TokenKind.Index    => GetIndexValue(root, token.Index),
                _                  => null
            };
        }

        return root;
    }

    // ------------------------
    // Tokenization
    // ------------------------

    private enum TokenKind { Property, Key, Index }

    private readonly struct Token
    {
        public TokenKind Kind { get; }
        public string Value { get; }   // used for Property/Key
        public int Index { get; }      // used for Index

        private Token(TokenKind kind, string value, int index)
        {
            Kind = kind;
            Value = value;
            Index = index;
        }

        public static Token Property(string name) => new(TokenKind.Property, name, default);
        public static Token Key(string key)       => new(TokenKind.Key, key, default);
        public static Token Indexer(int idx)      => new(TokenKind.Index, string.Empty, idx);
    }

    private static IEnumerable<Token> Tokenize(string path)
    {
        int i = 0;

        void SkipWs()
        {
            while (i < path.Length && char.IsWhiteSpace(path[i])) i++;
        }

        SkipWs();

        while (i < path.Length)
        {
            SkipWs();

            // Optional dot between segments
            if (i < path.Length && path[i] == '.')
            {
                i++;
                continue;
            }

            if (i >= path.Length) yield break;

            char c = path[i];

            // Bracket segment: ["key"] or ['key'] or [123]
            if (c == '[')
            {
                i++; // consume '['
                SkipWs();

                if (i >= path.Length) yield break;

                // Quoted key
                if (path[i] == '"' || path[i] == '\'')
                {
                    char quote = path[i++];
                    var sb = new StringBuilder();

                    while (i < path.Length)
                    {
                        char ch = path[i++];

                        if (ch == '\\' && i < path.Length)
                        {
                            // basic escape handling: \" \' \\ \n \r \t
                            char esc = path[i++];
                            sb.Append(esc switch
                            {
                                '\\' => '\\',
                                '"'  => '"',
                                '\'' => '\'',
                                'n'  => '\n',
                                'r'  => '\r',
                                't'  => '\t',
                                _    => esc
                            });
                            continue;
                        }

                        if (ch == quote)
                            break;

                        sb.Append(ch);
                    }

                    SkipWs();
                    if (i < path.Length && path[i] == ']') i++; // consume ']'

                    yield return Token.Key(sb.ToString());
                    continue;
                }

                // Numeric index
                {
                    int start = i;
                    bool neg = false;

                    if (i < path.Length && path[i] == '-')
                    {
                        neg = true;
                        i++;
                    }

                    while (i < path.Length && char.IsDigit(path[i])) i++;

                    var numStr = path.Substring(start, i - start);
                    SkipWs();
                    if (i < path.Length && path[i] == ']') i++; // consume ']'

                    if (!int.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx))
                        yield break;

                    if (neg) idx = -idx;

                    yield return Token.Indexer(idx);
                    continue;
                }
            }

            // Identifier property: inner, arr, z (C#-like identifier)
            if (IsIdentStart(c))
            {
                int start = i++;
                while (i < path.Length && IsIdentPart(path[i])) i++;

                string name = path.Substring(start, i - start);
                yield return Token.Property(name);
                continue;
            }

            // Unknown character; stop (or you could throw)
            yield break;
        }

        static bool IsIdentStart(char ch) => char.IsLetter(ch) || ch == '_';
        static bool IsIdentPart(char ch) => char.IsLetterOrDigit(ch) || ch == '_';
    }

    // ------------------------
    // Resolution
    // ------------------------

    private static object? GetMemberValue(object obj, string name, PathResolverOptions opt)
    {
        switch (obj)
        {
            // ---------- JsonElement ----------
            case JsonElement { ValueKind: JsonValueKind.Object } je:
            {
                if (opt.CaseSensitive)
                {
                    if (je.TryGetProperty(name, out var p)) return p;
                }
                else
                {
                    foreach (var prop in je.EnumerateObject())
                        if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                            return prop.Value;
                }
                return null;
            }
            // ---------- IDictionary<string, object> ----------
            case IDictionary<string, object> dictSo when opt.CaseSensitive:
                return dictSo.TryGetValue(name, out var v) ? v : null;
            case IDictionary<string, object> dictSo:
            {
                foreach (var kv in dictSo)
                    if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
                        return kv.Value;

                return null;
            }
            // ---------- IDictionary ----------
            case IDictionary dict when opt.CaseSensitive:
                return dict.Contains(name) ? dict[name] : null;
            case IDictionary dict:
            {
                foreach (DictionaryEntry de in dict)
                    if (de.Key is string k &&
                        string.Equals(k, name, StringComparison.OrdinalIgnoreCase))
                        return de.Value;

                return null;
            }
        }

        // ---------- POCO ----------
        var flags = BindingFlags.Instance | BindingFlags.Public |
            (opt.CaseSensitive ? BindingFlags.Default : BindingFlags.IgnoreCase);

        var propInfo = obj.GetType().GetProperty(name, flags);
        if (propInfo != null) return propInfo.GetValue(obj);

        var fieldInfo = obj.GetType().GetField(name, flags);
        if (fieldInfo != null) return fieldInfo.GetValue(obj);

        return null;
    }

    private static object? GetKeyValue(object obj, string key, PathResolverOptions opt)
    {
        switch (obj)
        {
            // ---------- JsonElement ----------
            case JsonElement { ValueKind: JsonValueKind.Object } je:
            {
                if (opt.CaseSensitive)
                {
                    if (je.TryGetProperty(key, out var p)) return p;
                }
                else
                {
                    foreach (var prop in je.EnumerateObject())
                        if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase))
                            return prop.Value;
                }
                return null;
            }
            // ---------- IDictionary<string, object> ----------
            case IDictionary<string, object> dictSo when opt.CaseSensitive:
                return dictSo.TryGetValue(key, out var v) ? v : null;
            case IDictionary<string, object> dictSo:
            {
                foreach (var kv in dictSo)
                    if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                        return kv.Value;

                return null;
            }
            // ---------- IDictionary ----------
            case IDictionary dict when opt.CaseSensitive:
                return dict.Contains(key) ? dict[key] : null;
            case IDictionary dict:
            {
                foreach (DictionaryEntry de in dict)
                    if (de.Key is string k &&
                        string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                        return de.Value;

                return null;
            }
            default:
                return null;
        }
    }


    private static object? GetIndexValue(object obj, int index)
    {
        if (index < 0) return null;

        // System.Text.Json: array index
        if (obj is JsonElement je)
        {
            if (je.ValueKind != JsonValueKind.Array) return null;

            var i = 0;
            foreach (var el in je.EnumerateArray())
            {
                if (i++ == index) return el;
            }
            return null;
        }

        if (obj is IList list)
            return index < list.Count ? list[index] : null;

        if (obj is Array arr)
            return index < arr.Length ? arr.GetValue(index) : null;

        return null;
    }
}
