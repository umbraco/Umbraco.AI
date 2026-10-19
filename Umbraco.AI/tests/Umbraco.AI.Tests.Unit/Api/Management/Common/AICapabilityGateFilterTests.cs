using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Common.Filters;
using Umbraco.AI.Web.Api.Management.Decision.Controllers;
using Umbraco.AI.Web.Api.Management.ImageGeneration.Controllers;

namespace Umbraco.AI.Tests.Unit.Api.Management.Common;

public class AICapabilityGateFilterTests
{
    private static ResourceExecutingContext CreateContext()
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        return new ResourceExecutingContext(actionContext, [], []);
    }

    private static AICapabilityGateFilter CreateFilter(AICapability capability, bool enabled)
    {
        var experimental = new Mock<IAIExperimentalFeatures>();
        experimental.Setup(x => x.IsCapabilityEnabled(capability)).Returns(enabled);
        return new AICapabilityGateFilter(capability, experimental.Object);
    }

    public class GivenTheFlagIsOff
    {
        private readonly ResourceExecutingContext _context = CreateContext();
        private bool _nextInvoked;

        public GivenTheFlagIsOff()
        {
            var filter = CreateFilter(AICapability.Decision, enabled: false);

            ResourceExecutionDelegate next = () =>
            {
                _nextInvoked = true;
                return Task.FromResult(new ResourceExecutedContext(_context, _context.Filters));
            };

            filter.OnResourceExecutionAsync(_context, next).GetAwaiter().GetResult();
        }

        [Fact]
        public void SetsAnEmptyNotFoundResult() => _context.Result.ShouldBeOfType<NotFoundResult>();

        [Fact]
        public void NeverInvokesNext() => _nextInvoked.ShouldBeFalse();
    }

    public class GivenTheFlagIsOn
    {
        [Fact]
        public async Task InvokesNext()
        {
            var context = CreateContext();
            var filter = CreateFilter(AICapability.Decision, enabled: true);
            var nextInvoked = false;

            ResourceExecutionDelegate next = () =>
            {
                nextInvoked = true;
                return Task.FromResult(new ResourceExecutedContext(context, context.Filters));
            };

            await filter.OnResourceExecutionAsync(context, next);

            nextInvoked.ShouldBeTrue();
        }
    }

    /// <summary>
    /// The filter is parameterised by <see cref="AICapability"/>, so a capability other than the one
    /// wired up must not incorrectly pass — proves the gate actually checks the capability it was
    /// constructed with, not just "some" capability.
    /// </summary>
    public class GivenADifferentCapabilityIsEnabled
    {
        [Fact]
        public async Task StillReturnsNotFoundForTheGatedCapability()
        {
            var context = CreateContext();
            var experimental = new Mock<IAIExperimentalFeatures>();
            experimental.Setup(x => x.IsCapabilityEnabled(AICapability.ImageGeneration)).Returns(true);
            experimental.Setup(x => x.IsCapabilityEnabled(AICapability.Decision)).Returns(false);
            var filter = new AICapabilityGateFilter(AICapability.Decision, experimental.Object);

            ResourceExecutionDelegate next = () => Task.FromResult(new ResourceExecutedContext(context, context.Filters));

            await filter.OnResourceExecutionAsync(context, next);

            context.Result.ShouldBeOfType<NotFoundResult>();
        }
    }

    /// <summary>
    /// Each gated controller base must carry <see cref="AICapabilityGateAttribute"/> for its own
    /// capability, so a future controller added under that base inherits the gate automatically.
    /// </summary>
    public class GivenAGatedControllerBase
    {
        [Fact]
        public void DecisionControllerBaseIsGatedOnDecision()
            => GetGateCapability(typeof(DecisionControllerBase)).ShouldBe(AICapability.Decision);

        [Fact]
        public void ImageGenerationControllerBaseIsGatedOnImageGeneration()
            => GetGateCapability(typeof(ImageGenerationControllerBase)).ShouldBe(AICapability.ImageGeneration);

        private static AICapability GetGateCapability(Type controllerBaseType)
        {
            var attribute = controllerBaseType
                .GetCustomAttributes<AICapabilityGateAttribute>(inherit: false)
                .ShouldHaveSingleItem();

            return attribute.Arguments!.OfType<AICapability>().ShouldHaveSingleItem();
        }
    }
}
