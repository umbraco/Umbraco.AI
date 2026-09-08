using System.Text;
using Umbraco.AI.Core.FileProcessing;

namespace Umbraco.AI.Tests.Unit.FileProcessing;

public class PlainTextFileProcessingHandlerTests
{
    private readonly PlainTextFileProcessingHandler _handler = new();

    #region CanHandle

    [Theory]
    [InlineData("text/plain", true)]
    [InlineData("text/csv", true)]
    [InlineData("text/markdown", true)]
    [InlineData("application/pdf", false)]
    [InlineData("image/png", false)]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", false)]
    [InlineData("application/octet-stream", false)]
    public async Task CanHandleAsync_WithMimeType_ReturnsExpected(string mimeType, bool expected)
    {
        (await _handler.CanHandleAsync(mimeType)).ShouldBe(expected);
    }

    #endregion

    #region ProcessAsync

    [Fact]
    public async Task ProcessAsync_WithPlainText_ReturnsContentUnchanged()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Hello World\nSecond line");

        // Act
        var result = await _handler.ProcessAsync(data, "text/plain", "notes.txt");

        // Assert
        result.Content.ShouldBe("Hello World\nSecond line");
        result.WasTruncated.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_WithCsv_ReturnsRawCsvText()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("name,age\nAlice,30\nBob,25");

        // Act
        var result = await _handler.ProcessAsync(data, "text/csv", "people.csv");

        // Assert
        result.Content.ShouldBe("name,age\nAlice,30\nBob,25");
        result.WasTruncated.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_WithMarkdown_ReturnsRawMarkdownText()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("# Title\n\nSome **bold** text.");

        // Act
        var result = await _handler.ProcessAsync(data, "text/markdown", "readme.md");

        // Assert
        result.Content.ShouldBe("# Title\n\nSome **bold** text.");
        result.WasTruncated.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_WithUtf8Bom_StripsBomFromContent()
    {
        // Arrange - Excel's "CSV UTF-8" export always writes a BOM before the content
        var csvContent = "name,age\nAlice,30\nBob,25";
        var data = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csvContent)).ToArray();

        // Act
        var result = await _handler.ProcessAsync(data, "text/csv", "people.csv");

        // Assert
        result.Content[0].ShouldNotBe('﻿');
        result.Content.ShouldBe(csvContent);
        result.WasTruncated.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyFile_ReturnsEmptyContent()
    {
        // Arrange
        var data = Array.Empty<byte>();

        // Act
        var result = await _handler.ProcessAsync(data, "text/plain", "empty.txt");

        // Assert
        result.Content.ShouldBeEmpty();
        result.WasTruncated.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_WithLargeContent_TruncatesAndIndicates()
    {
        // Arrange - content exceeding 100K characters
        var data = Encoding.UTF8.GetBytes(new string('A', 110_000));

        // Act
        var result = await _handler.ProcessAsync(data, "text/plain", "big.txt");

        // Assert
        result.WasTruncated.ShouldBeTrue();
        result.Content.ShouldContain("[Content truncated due to size limits]");
        result.Content.Length.ShouldBeLessThan(110_000);
    }

    #endregion
}
