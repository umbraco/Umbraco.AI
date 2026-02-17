namespace Umbraco.AI.Core.Tools.Scopes;

/// <summary>
/// The content read scope. Tools that perform read operations on content items, such as fetching or analyzing content, should use this scope to ensure proper permission handling and grouping in the UI.
/// </summary>
[AIToolScope(ScopeId, Icon = "icon-article", Domain = "Content", ForEntityTypes = ["document"])]
public sealed class ContentReadScope : AIToolScopeBase
{
    /// <summary>
    /// The unique identifier for the content read scope. Tools that perform read operations on content items, such as fetching or analyzing content, should use this scope to ensure proper permission handling and grouping in the UI.
    /// </summary>
    public const string ScopeId = "content-read";
}

/// <summary>
/// The content write scope. Tools that perform write operations on content items, such as creating, updating, or deleting content, should use this scope to ensure proper permission handling and grouping in the UI.
/// </summary>
[AIToolScope(ScopeId, Icon = "icon-article", IsDestructive = true, Domain = "Content", ForEntityTypes = ["document"])]
public sealed class ContentWriteScope : AIToolScopeBase
{
    /// <summary>
    /// The unique identifier for the content write scope. Tools that perform write operations on content items, such as creating, updating, or deleting content, should use this scope to ensure proper permission handling and grouping in the UI.
    /// </summary>
    public const string ScopeId = "content-write";
}

/// <summary>
/// The media read scope. Tools that perform read operations on media items, such as fetching or analyzing media, should use this scope to ensure proper permission handling and grouping in the UI.
/// </summary>
[AIToolScope(ScopeId, Icon = "icon-picture", Domain = "Media", ForEntityTypes = ["media"])]
public sealed class MediaReadScope : AIToolScopeBase
{
    /// <summary>
    /// The unique identifier for the media read scope. Tools that perform read operations on media items, such as fetching or analyzing media, should use this scope to ensure proper permission handling and grouping in the UI.
    /// </summary>
    public const string ScopeId = "media-read";
}

/// <summary>
/// The media write scope. Tools that perform write operations on media items, such as creating, updating, or deleting media, should use this scope to ensure proper permission handling and grouping in the UI.
/// </summary>
[AIToolScope(ScopeId, Icon = "icon-picture", IsDestructive = true, Domain = "Media", ForEntityTypes = ["media"])]
public sealed class MediaWriteScope : AIToolScopeBase
{
    /// <summary>
    /// The unique identifier for the media write scope. Tools that perform write operations on media items, such as creating, updating, or deleting media, should use this scope to ensure proper permission handling and grouping in the UI.
    /// </summary>
    public const string ScopeId = "media-write";
}

/// <summary>
/// The search scope. Tools that perform operations related to search, such as searching content or performing search-related analysis, should use this scope to ensure proper permission handling and grouping in the UI.
/// </summary>
[AIToolScope(ScopeId, Icon = "icon-search", Domain = "General")]
public sealed class SearchScope : AIToolScopeBase
{
    /// <summary>
    /// The unique identifier for the search scope. Tools that perform operations related to search, such as searching content or performing search-related analysis, should use this scope to ensure proper permission handling and grouping in the UI.
    /// </summary>
    public const string ScopeId = "search";
}

/// <summary>
/// The navigation scope. Tools that perform operations related to navigation, such as site structure analysis or link management, should use this scope to ensure proper permission handling and grouping in the UI.
/// </summary>
[AIToolScope(ScopeId, Icon = "icon-sitemap", Domain = "General")]
public sealed class NavigationScope : AIToolScopeBase
{
    /// <summary>
    /// The unique identifier for the navigation scope. Tools that perform operations related to navigation, such as site structure analysis or link management, should use this scope to ensure proper permission handling and grouping in the UI.
    /// </summary>
    public const string ScopeId = "navigation";
}

/// <summary>
/// The translation scope. Tools that perform operations related to translation, such as translating content or providing multilingual support, should use this scope to ensure proper permission handling and grouping in the UI.
/// </summary>
[AIToolScope(ScopeId, Icon = "icon-globe", Domain = "General")]
public sealed class TranslationScope : AIToolScopeBase
{
    /// <summary>
    /// The unique identifier for the translation scope. Tools that perform operations related to translation, such as translating content or providing multilingual support, should use this scope to ensure proper permission handling and grouping in the UI.
    /// </summary>
    public const string ScopeId = "translation";
}

/// <summary>
/// The web scope. Tools that perform operations related to web content, such as fetching data from the web or performing web-related analysis, should use this scope to ensure proper permission handling and grouping in the UI.
/// </summary>
[AIToolScope(ScopeId, Icon = "icon-globe", Domain = "General")]
public sealed class WebScope : AIToolScopeBase
{
    /// <summary>
    /// The unique identifier for the web scope. Tools that perform operations related to web content, such as fetching data from the web or performing web-related analysis, should use this scope to ensure proper permission handling and grouping in the UI.
    /// </summary>
    public const string ScopeId = "web";
}

/// <summary>
/// The entity read scope. Tools that perform read operations on entities, such as fetching or analyzing entities, should use this scope to ensure proper permission handling and grouping in the UI.
/// </summary>
[AIToolScope(ScopeId, Icon = "icon-document", Domain = "Entity")]
public sealed class EntityReadScope : AIToolScopeBase
{
    /// <summary>
    /// The unique identifier for the entity read scope. Tools that perform read operations on entities should use this scope to ensure proper permission handling and grouping in the UI.
    /// </summary>
    public const string ScopeId = "entity-read";
}

/// <summary>
/// The entity write scope. Tools that perform write operations on entities, such as creating, updating, or deleting entities, should use this scope to ensure proper permission handling and grouping in the UI.
/// </summary>
[AIToolScope(ScopeId, Icon = "icon-document", IsDestructive = true, Domain = "Entity")]
public sealed class EntityWriteScope : AIToolScopeBase
{
    /// <summary>
    /// The unique identifier for the entity write scope. Tools that perform write operations on entities should use this scope to ensure proper permission handling and grouping in the UI.
    /// </summary>
    public const string ScopeId = "entity-write";
}
