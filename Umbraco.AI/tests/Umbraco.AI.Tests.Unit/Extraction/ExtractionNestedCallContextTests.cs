#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability surface

// EX-8 — Extraction calls are tracked like other capabilities (AC8): a nested call gets its own runtime context.
//
// ASSUMPTION (builder confirms/adjusts): ScopedInlineExtractionClient exists alongside
// ScopedProfileExtractionClient, copying ScopedInlineDecisionClient, with the same constructor shape.

using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Extraction;

/// <summary>
/// Mirrors <c>AIRuntimeContextFlowTests.ScopedInlineDecisionClient_NestedInARunningCall_...</c> (#573/#575) with
/// the real <see cref="AIRuntimeContextScopeProvider"/>: an extraction made from inside another running tracked
/// call (an agent tool, an Automate step) must see its own context and leave the outer call's alone.
/// </summary>
public class ExtractionNestedCallContextTests
{
    #region Happy path

    public class GivenAnExtractionMadeInsideARunningChatCall
    {
        private readonly AIRuntimeContextScopeProvider _scopeProvider = new();
        private readonly AIRuntimeContextContributorCollection _contributors = new(() => []);

        private readonly string? _seenProfileAlias;
        private readonly string? _seenFeatureAlias;
        private readonly string? _outerProfileAliasAfter;
        private readonly string? _outerFeatureAliasAfter;

        public GivenAnExtractionMadeInsideARunningChatCall()
        {
            using var outerScope = _scopeProvider.CreateScope();
            outerScope.Context.SetValue(Constants.ContextKeys.ProfileAlias, "outer-profile");
            outerScope.Context.SetValue(Constants.ContextKeys.FeatureAlias, "outer-feature");
            var tracker = new AIOperationTracker(_scopeProvider, [], NullLogger<AIOperationTracker>.Instance);
            var running = tracker.BeginAsync(new AIOperationDescriptor { Capability = AICapability.Chat }, CancellationToken.None)
                .GetAwaiter().GetResult();

            string? seenProfileAlias = null;
            string? seenFeatureAlias = null;
            var inner = new FakeExtractionClient(request =>
            {
                seenProfileAlias = _scopeProvider.Context?.GetValue<string>(Constants.ContextKeys.ProfileAlias);
                seenFeatureAlias = _scopeProvider.Context?.GetValue<string>(Constants.ContextKeys.FeatureAlias);
                return new AIExtractionResponse
                {
                    Answers = request.Fields.ToDictionary(
                        f => f.Id ?? "answer", _ => (AIExtractionAnswer)new AITextExtractionAnswer { Value = "A-88412" }),
                };
            });
            var profile = new AIProfileBuilder()
                .WithAlias("extractor")
                .WithCapability(AICapability.Extraction)
                .WithModel("cloudflare", "@cf/cloudflare/clairic")
                .Build();
            var profileClient = new ScopedProfileExtractionClient(inner, profile, _scopeProvider, _scopeProvider, _contributors);
            var client = new ScopedInlineExtractionClient(
                profileClient, new AIExtractionBuilder().WithAlias("order-extract"), _scopeProvider, _scopeProvider, _contributors);

            using (running.EnterScope())
            {
                client.GetResponseAsync(new AIExtractionRequest
                {
                    State = "Order A-88412",
                    Fields = [new AITextExtractionField { Id = "orderId", Instructions = "The order number" }],
                }).GetAwaiter().GetResult();
            }

            _seenProfileAlias = seenProfileAlias;
            _seenFeatureAlias = seenFeatureAlias;
            _outerProfileAliasAfter = outerScope.Context.GetValue<string>(Constants.ContextKeys.ProfileAlias);
            _outerFeatureAliasAfter = outerScope.Context.GetValue<string>(Constants.ContextKeys.FeatureAlias);
            running.CompleteAsync(usage: null, responseData: null).GetAwaiter().GetResult();
        }

        [Fact]
        public void SeesItsOwnProfile() => _seenProfileAlias.ShouldBe("extractor");

        [Fact]
        public void SeesItsOwnFeature() => _seenFeatureAlias.ShouldBe("order-extract");

        [Fact]
        public void LeavesTheOuterCallsProfileAlone() => _outerProfileAliasAfter.ShouldBe("outer-profile");

        [Fact]
        public void LeavesTheOuterCallsFeatureAlone() => _outerFeatureAliasAfter.ShouldBe("outer-feature");
    }

    #endregion
}
