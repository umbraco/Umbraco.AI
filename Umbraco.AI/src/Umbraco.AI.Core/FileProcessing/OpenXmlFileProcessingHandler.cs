using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;

using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;
using Table = DocumentFormat.OpenXml.Wordprocessing.Table;
using TableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;
using TableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;

namespace Umbraco.AI.Core.FileProcessing;

/// <summary>
/// Extracts text content from Office Open XML documents (docx, xlsx, pptx).
/// </summary>
internal sealed class OpenXmlFileProcessingHandler : IAIFileProcessingHandler
{
    private const int MaxCharacters = 100_000;

    private static readonly HashSet<string> SupportedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
    };

    /// <inheritdoc />
    public bool CanHandle(string mimeType) => SupportedMimeTypes.Contains(mimeType);

    /// <inheritdoc />
    public Task<AIFileProcessingResult> ProcessAsync(
        ReadOnlyMemory<byte> data,
        string mimeType,
        string? filename,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(data.ToArray());

        var content = mimeType.ToLowerInvariant() switch
        {
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ExtractDocx(stream),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ExtractXlsx(stream),
            "application/vnd.openxmlformats-officedocument.presentationml.presentation" => ExtractPptx(stream),
            _ => string.Empty,
        };

        var wasTruncated = content.Length > MaxCharacters;
        if (wasTruncated)
        {
            content = content[..MaxCharacters] + "\n\n[Content truncated due to size limits]";
        }

        return Task.FromResult(new AIFileProcessingResult(content, wasTruncated));
    }

    private static string ExtractDocx(Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        foreach (var element in body.ChildElements)
        {
            switch (element)
            {
                case Paragraph paragraph:
                    AppendParagraph(sb, paragraph);
                    break;
                case Table table:
                    AppendWordTable(sb, table);
                    break;
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendParagraph(StringBuilder sb, Paragraph paragraph)
    {
        var style = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        var text = GetParagraphText(paragraph);

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (style is not null && style.StartsWith("Heading", StringComparison.OrdinalIgnoreCase))
        {
            // Try to extract heading level from style name (e.g., "Heading1" -> 1)
            var levelStr = style.AsSpan(7);
            var level = int.TryParse(levelStr, out var parsed) ? parsed : 1;
            level = Math.Clamp(level, 1, 6);

            sb.Append(new string('#', level));
            sb.Append(' ');
        }

        sb.AppendLine(text);
        sb.AppendLine();
    }

    private static string GetParagraphText(Paragraph paragraph)
    {
        var sb = new StringBuilder();
        foreach (var run in paragraph.Descendants<Run>())
        {
            foreach (var text in run.Descendants<Text>())
            {
                sb.Append(text.Text);
            }
        }

        return sb.ToString();
    }

    private static void AppendWordTable(StringBuilder sb, Table table)
    {
        var rows = table.Elements<TableRow>().ToList();
        if (rows.Count == 0)
        {
            return;
        }

        var isFirstRow = true;
        foreach (var row in rows)
        {
            var cells = row.Elements<TableCell>()
                .Select(c => GetCellText(c).Replace("|", "\\|"))
                .ToList();

            sb.Append("| ");
            sb.Append(string.Join(" | ", cells));
            sb.AppendLine(" |");

            if (isFirstRow)
            {
                sb.Append("| ");
                sb.Append(string.Join(" | ", cells.Select(_ => "---")));
                sb.AppendLine(" |");
                isFirstRow = false;
            }
        }

        sb.AppendLine();
    }

    private static string GetCellText(TableCell cell)
    {
        var sb = new StringBuilder();
        foreach (var paragraph in cell.Elements<Paragraph>())
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(GetParagraphText(paragraph));
        }

        return sb.ToString().Trim();
    }

    private static string ExtractXlsx(Stream stream)
    {
        using var doc = SpreadsheetDocument.Open(stream, false);
        var workbookPart = doc.WorkbookPart;
        if (workbookPart is null)
        {
            return string.Empty;
        }

        // Build shared string table lookup
        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable?
            .Elements<SharedStringItem>()
            .Select(s => s.InnerText)
            .ToArray() ?? [];

        var sb = new StringBuilder();
        var sheets = workbookPart.Workbook.Sheets?.Elements<Sheet>().ToList() ?? [];

        foreach (var sheet in sheets)
        {
            var worksheetPart = (WorksheetPart?)workbookPart.GetPartById(sheet.Id!.Value!);
            if (worksheetPart is null)
            {
                continue;
            }

            var sheetName = sheet.Name?.Value;
            if (!string.IsNullOrWhiteSpace(sheetName))
            {
                sb.AppendLine($"## {sheetName}");
                sb.AppendLine();
            }

            var rows = worksheetPart.Worksheet.Descendants<Row>().ToList();
            if (rows.Count == 0)
            {
                continue;
            }

            var isFirstRow = true;
            foreach (var row in rows)
            {
                var cells = row.Elements<Cell>().ToList();
                var values = cells.Select(c => GetCellValue(c, sharedStrings).Replace("|", "\\|")).ToList();

                sb.Append("| ");
                sb.Append(string.Join(" | ", values));
                sb.AppendLine(" |");

                if (isFirstRow)
                {
                    sb.Append("| ");
                    sb.Append(string.Join(" | ", values.Select(_ => "---")));
                    sb.AppendLine(" |");
                    isFirstRow = false;
                }
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static string GetCellValue(Cell cell, string[] sharedStrings)
    {
        var value = cell.CellValue?.InnerText ?? string.Empty;

        if (cell.DataType?.Value == CellValues.SharedString
            && int.TryParse(value, out var index)
            && index >= 0 && index < sharedStrings.Length)
        {
            return sharedStrings[index];
        }

        return value;
    }

    private static string ExtractPptx(Stream stream)
    {
        using var doc = PresentationDocument.Open(stream, false);
        var presentationPart = doc.PresentationPart;
        if (presentationPart is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList() ?? [];

        for (var i = 0; i < slideIds.Count; i++)
        {
            var slidePart = (SlidePart?)presentationPart.GetPartById(slideIds[i].RelationshipId!.Value!);
            if (slidePart is null)
            {
                continue;
            }

            sb.AppendLine($"## Slide {i + 1}");
            sb.AppendLine();

            var shapes = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Presentation.Shape>().ToList();
            foreach (var shape in shapes)
            {
                var text = shape.TextBody?.InnerText;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text);
                    sb.AppendLine();
                }
            }
        }

        return sb.ToString().TrimEnd();
    }
}
