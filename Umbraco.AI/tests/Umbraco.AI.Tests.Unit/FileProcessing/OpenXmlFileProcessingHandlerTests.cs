using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Umbraco.AI.Core.FileProcessing;

using Body = DocumentFormat.OpenXml.Wordprocessing.Body;
using Cell = DocumentFormat.OpenXml.Spreadsheet.Cell;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using Row = DocumentFormat.OpenXml.Spreadsheet.Row;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace Umbraco.AI.Tests.Unit.FileProcessing;

public class OpenXmlFileProcessingHandlerTests
{
    private readonly OpenXmlFileProcessingHandler _handler = new();

    #region CanHandle

    [Theory]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", true)]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", true)]
    [InlineData("application/vnd.openxmlformats-officedocument.presentationml.presentation", true)]
    [InlineData("application/pdf", false)]
    [InlineData("image/png", false)]
    [InlineData("text/plain", false)]
    [InlineData("application/octet-stream", false)]
    public void CanHandle_WithMimeType_ReturnsExpected(string mimeType, bool expected)
    {
        _handler.CanHandle(mimeType).ShouldBe(expected);
    }

    #endregion

    #region Docx Processing

    [Fact]
    public async Task ProcessAsync_WithDocx_ExtractsParagraphs()
    {
        // Arrange
        var data = CreateDocx(doc =>
        {
            var body = doc.MainDocumentPart!.Document.Body!;
            body.Append(CreateParagraph("Hello World"));
            body.Append(CreateParagraph("Second paragraph"));
        });

        // Act
        var result = await _handler.ProcessAsync(
            data,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "test.docx");

        // Assert
        result.Content.ShouldContain("Hello World");
        result.Content.ShouldContain("Second paragraph");
        result.WasTruncated.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_WithDocxHeadings_FormatsAsMarkdown()
    {
        // Arrange
        var data = CreateDocx(doc =>
        {
            var body = doc.MainDocumentPart!.Document.Body!;
            body.Append(CreateParagraph("Main Title", "Heading1"));
            body.Append(CreateParagraph("Some content"));
            body.Append(CreateParagraph("Sub Title", "Heading2"));
        });

        // Act
        var result = await _handler.ProcessAsync(
            data,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "test.docx");

        // Assert
        result.Content.ShouldContain("# Main Title");
        result.Content.ShouldContain("## Sub Title");
    }

    [Fact]
    public async Task ProcessAsync_WithDocxTable_FormatsAsMarkdownTable()
    {
        // Arrange
        var data = CreateDocx(doc =>
        {
            var body = doc.MainDocumentPart!.Document.Body!;

            var table = new DocumentFormat.OpenXml.Wordprocessing.Table();
            table.Append(CreateWordTableRow("Name", "Age"));
            table.Append(CreateWordTableRow("Alice", "30"));
            body.Append(table);
        });

        // Act
        var result = await _handler.ProcessAsync(
            data,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "test.docx");

        // Assert
        result.Content.ShouldContain("| Name | Age |");
        result.Content.ShouldContain("| --- | --- |");
        result.Content.ShouldContain("| Alice | 30 |");
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyDocx_ReturnsEmptyContent()
    {
        // Arrange
        var data = CreateDocx(_ => { });

        // Act
        var result = await _handler.ProcessAsync(
            data,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "test.docx");

        // Assert
        result.Content.ShouldBeEmpty();
        result.WasTruncated.ShouldBeFalse();
    }

    #endregion

    #region Xlsx Processing

    [Fact]
    public async Task ProcessAsync_WithXlsx_ExtractsAsMarkdownTable()
    {
        // Arrange
        var data = CreateXlsx(doc =>
        {
            var workbookPart = doc.WorkbookPart!;
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData(
                new Row(
                    CreateCell("Product"),
                    CreateCell("Price")),
                new Row(
                    CreateCell("Widget"),
                    CreateCell("9.99"))));

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Data",
            });
        });

        // Act
        var result = await _handler.ProcessAsync(
            data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "test.xlsx");

        // Assert
        result.Content.ShouldContain("## Data");
        result.Content.ShouldContain("Product");
        result.Content.ShouldContain("Widget");
        result.Content.ShouldContain("9.99");
        result.WasTruncated.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_WithXlsxSharedStrings_ResolvesValues()
    {
        // Arrange
        var data = CreateXlsx(doc =>
        {
            var workbookPart = doc.WorkbookPart!;

            // Add shared string table
            var sharedStringPart = workbookPart.AddNewPart<SharedStringTablePart>();
            sharedStringPart.SharedStringTable = new SharedStringTable(
                new SharedStringItem(new DocumentFormat.OpenXml.Spreadsheet.Text("Shared Value")));

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData(
                new Row(
                    new Cell
                    {
                        DataType = CellValues.SharedString,
                        CellValue = new CellValue("0"),
                    })));

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1",
            });
        });

        // Act
        var result = await _handler.ProcessAsync(
            data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "test.xlsx");

        // Assert
        result.Content.ShouldContain("Shared Value");
    }

    #endregion

    #region Pptx Processing

    [Fact]
    public async Task ProcessAsync_WithPptx_ExtractsSlideText()
    {
        // Arrange
        var data = CreatePptx(doc =>
        {
            var presentationPart = doc.PresentationPart!;
            var slidePart = presentationPart.AddNewPart<SlidePart>();
            slidePart.Slide = new Slide(
                new CommonSlideData(
                    new ShapeTree(
                        new NonVisualGroupShapeProperties(
                            new NonVisualDrawingProperties { Id = 1, Name = "" },
                            new NonVisualGroupShapeDrawingProperties(),
                            new ApplicationNonVisualDrawingProperties()),
                        new GroupShapeProperties(),
                        new DocumentFormat.OpenXml.Presentation.Shape(
                            new DocumentFormat.OpenXml.Presentation.NonVisualShapeProperties(
                                new NonVisualDrawingProperties { Id = 2, Name = "Title" },
                                new DocumentFormat.OpenXml.Presentation.NonVisualShapeDrawingProperties(),
                                new ApplicationNonVisualDrawingProperties()),
                            new DocumentFormat.OpenXml.Presentation.ShapeProperties(),
                            new DocumentFormat.OpenXml.Presentation.TextBody(
                                new DocumentFormat.OpenXml.Drawing.BodyProperties(),
                                new DocumentFormat.OpenXml.Drawing.Paragraph(
                                    new DocumentFormat.OpenXml.Drawing.Run(
                                        new DocumentFormat.OpenXml.Drawing.Text("Slide Title"))))))));

            var slideIdList = presentationPart.Presentation.AppendChild(new SlideIdList());
            slideIdList.Append(new SlideId
            {
                Id = 256,
                RelationshipId = presentationPart.GetIdOfPart(slidePart),
            });
        });

        // Act
        var result = await _handler.ProcessAsync(
            data,
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "test.pptx");

        // Assert
        result.Content.ShouldContain("## Slide 1");
        result.Content.ShouldContain("Slide Title");
        result.WasTruncated.ShouldBeFalse();
    }

    #endregion

    #region Truncation

    [Fact]
    public async Task ProcessAsync_WithLargeContent_TruncatesAndIndicates()
    {
        // Arrange - create a docx with content exceeding 100K characters
        var data = CreateDocx(doc =>
        {
            var body = doc.MainDocumentPart!.Document.Body!;
            var longText = new string('A', 110_000);
            body.Append(CreateParagraph(longText));
        });

        // Act
        var result = await _handler.ProcessAsync(
            data,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "test.docx");

        // Assert
        result.WasTruncated.ShouldBeTrue();
        result.Content.ShouldContain("[Content truncated due to size limits]");
        result.Content.Length.ShouldBeLessThan(110_000);
    }

    #endregion

    #region Test Helpers

    private static ReadOnlyMemory<byte> CreateDocx(Action<WordprocessingDocument> configure)
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            configure(doc);
        }

        return new ReadOnlyMemory<byte>(stream.ToArray());
    }

    private static ReadOnlyMemory<byte> CreateXlsx(Action<SpreadsheetDocument> configure)
    {
        using var stream = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = doc.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            configure(doc);
        }

        return new ReadOnlyMemory<byte>(stream.ToArray());
    }

    private static ReadOnlyMemory<byte> CreatePptx(Action<PresentationDocument> configure)
    {
        using var stream = new MemoryStream();
        using (var doc = PresentationDocument.Create(stream, PresentationDocumentType.Presentation))
        {
            var presentationPart = doc.AddPresentationPart();
            presentationPart.Presentation = new Presentation();
            configure(doc);
        }

        return new ReadOnlyMemory<byte>(stream.ToArray());
    }

    private static Paragraph CreateParagraph(string text, string? styleId = null)
    {
        var paragraph = new Paragraph(new Run(new Text(text)));

        if (styleId is not null)
        {
            paragraph.PrependChild(new ParagraphProperties(
                new ParagraphStyleId { Val = styleId }));
        }

        return paragraph;
    }

    private static DocumentFormat.OpenXml.Wordprocessing.TableRow CreateWordTableRow(params string[] cellTexts)
    {
        var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
        foreach (var text in cellTexts)
        {
            row.Append(new DocumentFormat.OpenXml.Wordprocessing.TableCell(
                new Paragraph(new Run(new Text(text)))));
        }

        return row;
    }

    private static Cell CreateCell(string value)
    {
        return new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue(value),
        };
    }

    #endregion
}
