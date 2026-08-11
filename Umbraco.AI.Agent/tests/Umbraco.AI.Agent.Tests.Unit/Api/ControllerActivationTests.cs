using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Api;

/// <summary>
/// MVC creates controllers through <see cref="ActivatorUtilities"/>, which demands exactly one
/// applicable constructor and throws at request time — not at startup — when it can satisfy more than
/// one. Keeping an obsolete constructor alongside a new one is therefore enough to break every request
/// to that controller unless the intended one carries
/// <see cref="ActivatorUtilitiesConstructorAttribute"/>.
/// </summary>
/// <remarks>
/// This sweeps the whole controller surface rather than naming one type, so adding an overload to any
/// controller in future is caught here instead of in the browser.
/// </remarks>
public class ControllerActivationTests
{
    public static TheoryData<Type> ControllerTypes()
    {
        var data = new TheoryData<Type>();

        var controllers = typeof(StreamAgentAGUIController).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsPublic: true } && typeof(ControllerBase).IsAssignableFrom(t))
            .OrderBy(t => t.FullName);

        foreach (var controller in controllers)
        {
            data.Add(controller);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ControllerTypes))]
    public void Controller_HasExactlyOneActivatableConstructor(Type controllerType)
    {
        // Act & Assert
        // Mirrors what ControllerActivatorProvider does per request. Building the factory is enough —
        // it throws on ambiguity without needing any service to be registered.
        Should.NotThrow(
            () => ActivatorUtilities.CreateFactory(controllerType, Type.EmptyTypes),
            $"{controllerType.Name} cannot be activated by MVC. If it has more than one constructor, "
                + "mark the intended one with [ActivatorUtilitiesConstructor].");
    }
}
