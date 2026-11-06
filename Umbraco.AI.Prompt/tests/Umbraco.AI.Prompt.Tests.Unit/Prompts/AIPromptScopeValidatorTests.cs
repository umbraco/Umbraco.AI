using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Prompts;

public class AIPromptScopeValidatorTests
{
    private readonly Mock<IContentTypeService> _contentTypeService = new();
    private readonly Mock<IDataTypeService> _dataTypeService = new();

    private AIPromptScopeValidator CreateValidator(string dataTypeEditorUiAlias)
    {
        var dataTypeKey = Guid.NewGuid();

        var propertyType = new Mock<IPropertyType>();
        propertyType.SetupGet(p => p.Alias).Returns("image");
        propertyType.SetupGet(p => p.DataTypeKey).Returns(dataTypeKey);

        var contentType = new Mock<IContentType>();
        contentType.SetupGet(c => c.CompositionPropertyTypes).Returns([propertyType.Object]);
        _contentTypeService.Setup(s => s.Get("page")).Returns(contentType.Object);

        var dataType = new Mock<IDataType>();
        dataType.SetupGet(d => d.EditorUiAlias).Returns(dataTypeEditorUiAlias);
        _dataTypeService.Setup(s => s.GetAsync(dataTypeKey)).ReturnsAsync(dataType.Object);

        return new AIPromptScopeValidator(
            _contentTypeService.Object,
            new Mock<IMediaTypeService>().Object,
            new Mock<IMemberTypeService>().Object,
            _dataTypeService.Object,
            NullLogger<AIPromptScopeValidator>.Instance);
    }

    private static AIPrompt CreatePrompt(string ruleAlias) => new()
    {
        Alias = "p",
        Name = "P",
        Instructions = "i",
        Scope = new AIPromptScope
        {
            AllowRules = [new AIPromptScopeRule { PropertyEditorUiAliases = [ruleAlias] }],
        },
    };

    private static readonly AIPromptExecutionRequest _request = new()
    {
        EntityId = Guid.NewGuid(),
        EntityType = "document",
        PropertyAlias = "image",
        ContentTypeAlias = "page",
    };

    [Fact]
    public async Task ValidateAsync_OldAliasRule_DataTypeOnSplitAlias_IsAllowed()
    {
        var validator = CreateValidator("Umb.PropertyEditorUi.MediaPicker.Single");

        var result = await validator.ValidateAsync(CreatePrompt("Umb.PropertyEditorUi.MediaPicker"), _request);

        result.IsAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_NewAliasRule_DataTypeOnOldAlias_IsDenied()
    {
        var validator = CreateValidator("Umb.PropertyEditorUi.MediaPicker");

        var result = await validator.ValidateAsync(CreatePrompt("Umb.PropertyEditorUi.MediaPicker.Single"), _request);

        result.IsAllowed.ShouldBeFalse();
    }
}
