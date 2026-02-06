namespace Umbraco.AI.Core.Tools.Scopes;

// Content scopes
[AIToolScope("content-read", Icon = "icon-article", Domain = "Content")]
public sealed class ContentReadScope : AIToolScopeBase
{
}

[AIToolScope("content-write", Icon = "icon-article", IsDestructive = true, Domain = "Content")]
public sealed class ContentWriteScope : AIToolScopeBase
{
}

// Media scopes
[AIToolScope("media-read", Icon = "icon-picture", Domain = "Media")]
public sealed class MediaReadScope : AIToolScopeBase
{
}

[AIToolScope("media-write", Icon = "icon-picture", IsDestructive = true, Domain = "Media")]
public sealed class MediaWriteScope : AIToolScopeBase
{
}

// General scopes
[AIToolScope("search", Icon = "icon-search", Domain = "General")]
public sealed class SearchScope : AIToolScopeBase
{
}

[AIToolScope("navigation", Icon = "icon-sitemap", Domain = "General")]
public sealed class NavigationScope : AIToolScopeBase
{
}

[AIToolScope("translation", Icon = "icon-globe", Domain = "General")]
public sealed class TranslationScope : AIToolScopeBase
{
}

[AIToolScope("web", Icon = "icon-globe-inverted-americas-alt", Domain = "General")]
public sealed class WebScope : AIToolScopeBase
{
}

// Entity scopes
[AIToolScope("entity-read", Icon = "icon-document", Domain = "Entity")]
public sealed class EntityReadScope : AIToolScopeBase
{
}

[AIToolScope("entity-write", Icon = "icon-document", IsDestructive = true, Domain = "Entity")]
public sealed class EntityWriteScope : AIToolScopeBase
{
}
