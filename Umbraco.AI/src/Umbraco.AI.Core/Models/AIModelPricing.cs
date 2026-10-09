namespace Umbraco.AI.Core.Models;

/// <summary>
/// A model's price per million tokens, as declared by its provider.
/// </summary>
/// <param name="InputPerMillionTokens">The price of one million input tokens.</param>
/// <param name="OutputPerMillionTokens">The price of one million output tokens.</param>
/// <param name="Currency">The ISO 4217 currency code the prices are expressed in (e.g. <c>USD</c>).</param>
public sealed record AIModelPricing(decimal InputPerMillionTokens, decimal OutputPerMillionTokens, string Currency);
