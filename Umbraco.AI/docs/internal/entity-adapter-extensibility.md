# Entity Adapter Extensibility Guide

This guide explains how to create custom entity adapters for third-party systems, allowing AI agents to interact with any entity type in domain-appropriate formats.

## Overview

The entity adapter system provides a flexible way to serialize entities for LLM context. Instead of forcing all entities into a rigid property-based structure, adapters can use domain-appropriate JSON formats that best represent their data.

## Architecture

### Core Components

1. **Entity Adapters (Frontend)**: TypeScript classes implementing `UaiEntityAdapterApi` that detect and serialize entities
2. **Entity Formatters (Backend)**: C# classes implementing `IAIEntityFormatter` that format entities for LLM consumption
3. **AISerializedEntity**: The transfer object with a free-form `data` field

### Data Flow

```
User edits entity in Umbraco
    ↓
Adapter detects workspace context
    ↓
Adapter serializes entity → { entityType, unique, name, data }
    ↓
Backend receives serialized entity
    ↓
Formatter formats entity for LLM → Markdown string
    ↓
LLM receives formatted context
```

## Creating a Custom Adapter (Frontend)

### Example: Commerce Product Adapter

```typescript
import type {
    UaiEntityAdapterApi,
    UaiEntityContext,
    UaiSerializedEntity,
} from "@umbraco-ai/core";

export class UaiCommerceProductAdapter implements UaiEntityAdapterApi {
    readonly entityType = "commerce-product";

    canHandle(workspaceContext: unknown): boolean {
        // Duck-type check for commerce workspace
        const ctx = workspaceContext as any;
        return (
            typeof ctx?.getEntityType === "function" &&
            ctx.getEntityType() === "commerce-product"
        );
    }

    extractEntityContext(workspaceContext: unknown): UaiEntityContext {
        const ctx = workspaceContext as any;
        return {
            entityType: "commerce-product",
            unique: ctx.getSku() ?? null,
        };
    }

    getName(workspaceContext: unknown): string {
        const ctx = workspaceContext as any;
        return ctx.getName() ?? "Untitled Product";
    }

    async serializeForLlm(workspaceContext: unknown): Promise<UaiSerializedEntity> {
        const ctx = workspaceContext as any;

        // Serialize in domain-appropriate structure
        return {
            entityType: "commerce-product",
            unique: ctx.getSku() ?? "new",
            name: ctx.getName() ?? "Untitled Product",
            data: {
                // Free-form structure - not forced into properties array
                sku: ctx.getSku(),
                category: ctx.getCategory(),
                price: {
                    amount: ctx.getPrice(),
                    currency: ctx.getCurrency(),
                },
                inventory: {
                    inStock: ctx.isInStock(),
                    quantity: ctx.getStockQuantity(),
                },
                variants: ctx.getVariants().map((v: any) => ({
                    color: v.color,
                    size: v.size,
                    sku: v.sku,
                })),
            },
        };
    }
}
```

### Registering the Adapter

```typescript
// In your extension manifest
export const manifest: ManifestUaiEntityAdapter = {
    type: "uaiEntityAdapter",
    alias: "commerce.product",
    name: "Commerce Product Adapter",
    api: () => import("./product.adapter.js"),
};
```

## Creating a Custom Formatter (Backend)

### Example: Commerce Product Formatter

```csharp
using System.Text;
using System.Text.Json;
using Umbraco.AI.Core.EntityAdapter;

namespace MyCommerce.AI;

/// <summary>
/// Formatter for commerce product entities.
/// Presents product data in a structured format optimized for LLM comprehension.
/// </summary>
public class CommerceProductFormatter : IAIEntityFormatter
{
    /// <inheritdoc />
    public string? EntityType => "commerce-product";

    /// <inheritdoc />
    public string Format(AISerializedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var sb = new StringBuilder();

        sb.AppendLine($"## Current Product Context");
        sb.AppendLine($"SKU: `{entity.Unique}`");
        sb.AppendLine($"Name: `{entity.Name}`");
        sb.AppendLine();

        // Extract commerce-specific fields from data
        if (entity.Data.ValueKind == JsonValueKind.Object)
        {
            // Category
            if (entity.Data.TryGetProperty("category", out var category))
            {
                sb.AppendLine($"**Category:** {category.GetString()}");
            }

            // Price
            if (entity.Data.TryGetProperty("price", out var price) &&
                price.ValueKind == JsonValueKind.Object)
            {
                if (price.TryGetProperty("amount", out var amount) &&
                    price.TryGetProperty("currency", out var currency))
                {
                    sb.AppendLine($"**Price:** {amount.GetDouble():F2} {currency.GetString()}");
                }
            }

            // Inventory
            if (entity.Data.TryGetProperty("inventory", out var inventory) &&
                inventory.ValueKind == JsonValueKind.Object)
            {
                if (inventory.TryGetProperty("inStock", out var inStock))
                {
                    var stockStatus = inStock.GetBoolean() ? "In Stock" : "Out of Stock";
                    sb.AppendLine($"**Availability:** {stockStatus}");
                }

                if (inventory.TryGetProperty("quantity", out var quantity))
                {
                    sb.AppendLine($"**Quantity:** {quantity.GetInt32()}");
                }
            }

            // Variants
            if (entity.Data.TryGetProperty("variants", out var variants) &&
                variants.ValueKind == JsonValueKind.Array)
            {
                sb.AppendLine();
                sb.AppendLine("**Variants:**");
                foreach (var variant in variants.EnumerateArray())
                {
                    var color = variant.TryGetProperty("color", out var c) ? c.GetString() : "N/A";
                    var size = variant.TryGetProperty("size", out var s) ? s.GetString() : "N/A";
                    var sku = variant.TryGetProperty("sku", out var skuElement) ? skuElement.GetString() : "N/A";
                    sb.AppendLine($"- {color} / {size} (SKU: {sku})");
                }
            }
        }

        return sb.ToString();
    }
}
```

### Registering the Formatter

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using MyCommerce.AI;

public class MyCommerceComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Register the formatter
        builder.AIEntityFormatters()
            .Add<CommerceProductFormatter>();
    }
}
```

## CMS Entity Structure (Document/Media)

For backward compatibility, CMS document and media entities continue to use a property-based structure **nested inside the data field**:

```typescript
{
    entityType: "document",
    unique: "guid-here",
    name: "Page Title",
    data: {
        contentType: "blogPost",
        properties: [
            {
                alias: "title",
                label: "Title",
                editorAlias: "Umbraco.TextBox",
                value: "Hello World"
            }
        ]
    }
}
```

The `AIDocumentEntityFormatter` recognizes this structure and formats properties appropriately. If the structure doesn't match, it falls back to generic JSON formatting.

## Forms Entry Example

Here's how you might structure a Forms entry entity:

```typescript
// Adapter serialization
{
    entityType: "forms-entry",
    unique: "entry-12345",
    name: "Contact Form Submission",
    data: {
        formId: "contact-form",
        formName: "Contact Us",
        submittedAt: "2026-02-12T10:30:00Z",
        fields: [
            { label: "Name", value: "John Smith" },
            { label: "Email", value: "john@example.com" },
            { label: "Message", value: "I'd like to know more..." }
        ]
    }
}
```

```csharp
// Custom formatter
public class FormsEntryFormatter : IAIEntityFormatter
{
    public string? EntityType => "forms-entry";

    public string Format(AISerializedEntity entity)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Form Submission");
        sb.AppendLine($"Entry ID: `{entity.Unique}`");

        if (entity.Data.TryGetProperty("formName", out var formName))
        {
            sb.AppendLine($"Form: {formName.GetString()}");
        }

        if (entity.Data.TryGetProperty("submittedAt", out var submittedAt))
        {
            sb.AppendLine($"Submitted: {submittedAt.GetString()}");
        }

        if (entity.Data.TryGetProperty("fields", out var fields) &&
            fields.ValueKind == JsonValueKind.Array)
        {
            sb.AppendLine();
            sb.AppendLine("### Submitted Data");
            foreach (var field in fields.EnumerateArray())
            {
                var label = field.GetProperty("label").GetString();
                var value = field.GetProperty("value").GetString();
                sb.AppendLine($"- **{label}:** {value}");
            }
        }

        return sb.ToString();
    }
}
```

## Best Practices

### Adapter Design

1. **Use domain-appropriate structure**: Don't force your data into properties if a different structure is more natural
2. **Keep it simple**: Only serialize data that's relevant for AI context
3. **Include metadata**: Add fields like timestamps, categories, or status that help the LLM understand context
4. **Handle nulls gracefully**: Ensure your adapter works with new (unsaved) entities

### Formatter Design

1. **Optimize for LLM comprehension**: Format data in a way that's easy for the LLM to understand
2. **Use markdown formatting**: Headers, lists, and code blocks improve readability
3. **Provide context**: Include explanatory text ("When the user says 'this product'...")
4. **Handle missing data**: Use default values or omit sections when data isn't available
5. **Consider token efficiency**: Balance completeness with brevity

### Data Structure Guidelines

```typescript
// ✅ GOOD - Clear, domain-appropriate structure
{
    entityType: "product",
    data: {
        sku: "WIDGET-123",
        category: "electronics",
        price: { amount: 29.99, currency: "USD" },
        variants: [...]
    }
}

// ❌ AVOID - Forcing into property structure when not natural
{
    entityType: "product",
    data: {
        properties: [
            { alias: "sku", label: "SKU", value: "WIDGET-123" },
            { alias: "category", label: "Category", value: "electronics" },
            { alias: "price", label: "Price", value: "29.99 USD" }
        ]
    }
}
```

## Fallback Behavior

If no entity-type-specific formatter is registered, the generic formatter pretty-prints the entire `data` field as a JSON code block. This ensures third-party entities work out-of-the-box, even without a custom formatter.

## Migration from Old Structure

If you have existing code using the old structure:

```typescript
// Old structure (deprecated)
{
    entityType: "document",
    contentType: "blogPost",  // ← Top level
    properties: [...]         // ← Top level
}

// New structure
{
    entityType: "document",
    data: {
        contentType: "blogPost",  // ← Nested
        properties: [...]         // ← Nested
    }
}
```

The backend automatically handles this by checking for `data.contentType` and `data.properties` when building context dictionaries.

## Additional Resources

- **Core types**: `Umbraco.AI.Core/EntityAdapter/`
- **Formatter examples**: `Umbraco.AI.Core/EntityAdapter/AIGenericEntityFormatter.cs`, `AIDocumentEntityFormatter.cs`
- **Frontend types**: `@umbraco-ai/core` package (`entity-adapter/types.ts`)
- **Document adapter**: `@umbraco-ai/core` (`entity-adapter/adapters/document.adapter.ts`)
