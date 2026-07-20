using System.Text;

namespace Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;

/// <summary>
/// Resource type for an uploaded project-knowledge file — its extracted text is injected as durable
/// context. Distinct from thread-scoped in-chat file uploads (which use the ephemeral file store).
/// </summary>
[AIContextResourceType("file", "File",
    Description = "Durable project-knowledge file whose extracted text grounds the AI",
    Icon = "icon-document")]
public sealed class FileResourceType : AIContextResourceTypeBase<FileResourceSettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileResourceType"/> class.
    /// </summary>
    /// <param name="infrastructure">The infrastructure dependencies.</param>
    public FileResourceType(IAIContextResourceTypeInfrastructure infrastructure)
        : base(infrastructure)
    { }

    /// <inheritdoc />
    protected override string FormatDataForLlm(FileResourceSettings data)
    {
        if (string.IsNullOrWhiteSpace(data.ExtractedText))
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.FileName))
        {
            sb.Append("# ").AppendLine(data.FileName);
        }

        sb.AppendLine(data.ExtractedText.Trim());
        return sb.ToString().Trim();
    }
}
