using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Web.Api.Management.Chat.Mapping;
using Umbraco.AI.Web.Api.Management.Chat.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Scoping;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Api.Management.Chat;

public class ChatMapDefinitionTests
{
    private readonly UmbracoMapper _mapper;

    public ChatMapDefinitionTests()
    {
        var definition = new ChatMapDefinition();
        _mapper = new UmbracoMapper(
            new MapDefinitionCollection(() => new IMapDefinition[] { definition }),
            Mock.Of<ICoreScopeProvider>(),
            NullLogger<UmbracoMapper>.Instance);
    }

#pragma warning disable CS0618 // Content is obsolete — testing backward compat

    [Fact]
    public void Map_PlainTextMessage_ProducesChatMessageWithTextContent()
    {
        var model = new ChatMessageModel { Role = "user", Content = "Hello world" };

        var result = _mapper.Map<ChatMessage>(model);

        result.ShouldNotBeNull();
        result!.Role.ShouldBe(ChatRole.User);
        result.Text.ShouldBe("Hello world");
    }

    [Theory]
    [InlineData("system")]
    [InlineData("user")]
    [InlineData("assistant")]
    public void Map_KnownRoles_ProducesCorrectChatRole(string inputRole)
    {
        var expected = inputRole switch
        {
            "system" => ChatRole.System,
            "user" => ChatRole.User,
            "assistant" => ChatRole.Assistant,
            _ => ChatRole.User
        };

        var model = new ChatMessageModel { Role = inputRole, Content = "test" };

        var result = _mapper.Map<ChatMessage>(model)!;

        result.Role.ShouldBe(expected);
    }

    [Fact]
    public void Map_UnknownRole_DefaultsToUser()
    {
        var model = new ChatMessageModel { Role = "unknown", Content = "test" };

        var result = _mapper.Map<ChatMessage>(model)!;

        result.Role.ShouldBe(ChatRole.User);
    }

    [Fact]
    public void Map_ContentPartsWithTextAndBinary_ProducesMultimodalMessage()
    {
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG magic bytes
        var base64 = Convert.ToBase64String(imageBytes);

        var model = new ChatMessageModel
        {
            Role = "user",
            ContentParts =
            [
                new TextChatContentPartModel { Text = "Describe this image" },
                new BinaryChatContentPartModel { MimeType = "image/png", Data = base64, Filename = "photo.png" }
            ]
        };

        var result = _mapper.Map<ChatMessage>(model)!;

        result.Role.ShouldBe(ChatRole.User);
        result.Contents.Count.ShouldBe(2);

        var textContent = result.Contents[0].ShouldBeOfType<TextContent>();
        textContent.Text.ShouldBe("Describe this image");

        var dataContent = result.Contents[1].ShouldBeOfType<DataContent>();
        dataContent.MediaType.ShouldBe("image/png");
        dataContent.Name.ShouldBe("photo.png");

        dataContent.Data.ToArray().ShouldBe(imageBytes);
    }

    [Fact]
    public void Map_BinaryPartWithoutFilename_SetsNameToNull()
    {
        var base64 = Convert.ToBase64String(new byte[] { 1, 2, 3 });

        var model = new ChatMessageModel
        {
            Role = "user",
            ContentParts =
            [
                new BinaryChatContentPartModel { MimeType = "application/pdf", Data = base64 }
            ]
        };

        var result = _mapper.Map<ChatMessage>(model)!;

        var dataContent = result.Contents[0].ShouldBeOfType<DataContent>();
        dataContent.Name.ShouldBeNull();
    }

    [Fact]
    public void Map_ContentPartsTakesPrecedenceOverContent()
    {
        var model = new ChatMessageModel
        {
            Role = "user",
            Content = "This should be ignored",
            ContentParts =
            [
                new TextChatContentPartModel { Text = "This should be used" }
            ]
        };

        var result = _mapper.Map<ChatMessage>(model)!;

        result.Contents.Count.ShouldBe(1);
        var textContent = result.Contents[0].ShouldBeOfType<TextContent>();
        textContent.Text.ShouldBe("This should be used");
    }

    [Fact]
    public void Map_EmptyContentParts_FallsBackToContent()
    {
        var model = new ChatMessageModel
        {
            Role = "user",
            Content = "Fallback text",
            ContentParts = []
        };

        var result = _mapper.Map<ChatMessage>(model)!;

        result.Text.ShouldBe("Fallback text");
    }

    [Fact]
    public void Map_NullContentParts_FallsBackToContent()
    {
        var model = new ChatMessageModel
        {
            Role = "user",
            Content = "Fallback text",
            ContentParts = null
        };

        var result = _mapper.Map<ChatMessage>(model)!;

        result.Text.ShouldBe("Fallback text");
    }

    [Fact]
    public void Map_NullContentAndNullContentParts_ProducesEmptyString()
    {
        var model = new ChatMessageModel
        {
            Role = "user",
            Content = null,
            ContentParts = null
        };

        var result = _mapper.Map<ChatMessage>(model)!;

        result.Text.ShouldBe(string.Empty);
    }

#pragma warning restore CS0618
}
