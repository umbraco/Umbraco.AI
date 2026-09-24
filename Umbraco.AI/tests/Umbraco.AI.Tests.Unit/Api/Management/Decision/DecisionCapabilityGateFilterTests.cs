using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Decision.Controllers;

namespace Umbraco.AI.Tests.Unit.Api.Management.Decision;

public class DecisionCapabilityGateFilterTests
{
    private static ResourceExecutingContext CreateContext()
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        return new ResourceExecutingContext(actionContext, [], []);
    }

    private static DecisionCapabilityGateFilter CreateFilter(bool enabled)
    {
        var experimental = new Mock<IAIExperimentalFeatures>();
        experimental.Setup(x => x.IsCapabilityEnabled(AICapability.Decision)).Returns(enabled);
        return new DecisionCapabilityGateFilter(experimental.Object);
    }

    public class GivenTheFlagIsOff
    {
        private readonly ResourceExecutingContext _context = CreateContext();
        private bool _nextInvoked;

        public GivenTheFlagIsOff()
        {
            var filter = CreateFilter(enabled: false);

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
            var filter = CreateFilter(enabled: true);
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
}
