using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Web.Api.Management.Prompt.Controllers;
using Umbraco.AI.Prompt.Web.Api.Management.Prompt.Models;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.Cms.Core.Mapping;
using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Api.Management.Prompt;

public class ExecutePromptControllerTests
{
    private static readonly PromptExecutionRequestModel RequestModel = new()
    {
        EntityId = Guid.NewGuid(),
        EntityType = "document",
        PropertyAlias = "title",
        ContentTypeAlias = "homePage",
    };

    private static ExecutePromptController CreateController(Mock<IAIPromptService> service)
    {
        var mapper = new Mock<IUmbracoMapper>();
        mapper
            .Setup(x => x.Map<AIPromptExecutionRequest>(It.IsAny<object>()))
            .Returns(new AIPromptExecutionRequest
            {
                EntityId = RequestModel.EntityId,
                EntityType = RequestModel.EntityType,
                PropertyAlias = RequestModel.PropertyAlias,
                ContentTypeAlias = RequestModel.ContentTypeAlias,
            });

        return new ExecutePromptController(service.Object, mapper.Object);
    }

    private static async Task<IActionResult> ExecuteThrowingAsync(Exception exception)
    {
        var promptId = Guid.NewGuid();
        var service = new Mock<IAIPromptService>();
        service
            .Setup(x => x.GetPromptAsync(promptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIPrompt { Alias = "summarize", Name = "Summarize", Instructions = "Summarize" });
        service
            .Setup(x => x.ExecutePromptAsync(promptId, It.IsAny<AIPromptExecutionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        return await CreateController(service).ExecutePrompt(new IdOrAlias(promptId), RequestModel);
    }

    // The two "not found" messages used to be reported as the prompt itself not existing.
    [Theory]
    [InlineData("Default Chat profile with alias 'default-chat' not found.")]
    [InlineData("AI profile with ID '00000000-0000-0000-0000-000000000000' not found.")]
    [InlineData("Default Chat profile is not configured.")]
    public async Task ExecutePrompt_WhenExecutionFails_ReturnsTheReasonWithAType(string message)
    {
        var result = await ExecuteThrowingAsync(new InvalidOperationException(message));

        var problem = result.ShouldBeOfType<BadRequestObjectResult>().Value.ShouldBeOfType<ProblemDetails>();
        problem.Detail.ShouldBe(message);
        problem.Type.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecutePrompt_WhenTheProviderFails_ReturnsItsUserMessage()
    {
        var result = await ExecuteThrowingAsync(new AIProviderException(new AIProviderErrorInfo(
            AIProviderErrorCategory.InvalidRequest, "The model does not exist.", null, "raw")));

        var problem = result.ShouldBeOfType<BadRequestObjectResult>().Value.ShouldBeOfType<ProblemDetails>();
        problem.Detail.ShouldBe("The model does not exist.");
    }

    [Fact]
    public async Task ExecutePrompt_WithAnUnknownPromptId_ReturnsNotFound()
    {
        var result = await CreateController(new Mock<IAIPromptService>())
            .ExecutePrompt(new IdOrAlias(Guid.NewGuid()), RequestModel);

        result.ShouldBeOfType<NotFoundObjectResult>();
    }
}
