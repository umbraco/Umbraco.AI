using Shouldly;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Tests.Unit.Settings;

public class AIEditableModelSchemaBuilderTests
{
    private const string MaskedTextBox = "Uai.PropertyEditorUi.MaskedTextBox";
    private const string TextBox = "Umb.PropertyEditorUi.TextBox";

    private readonly AIEditableModelSchemaBuilder _builder = new();

    private class TestSettings
    {
        [AIField(IsSensitive = true)]
        public string? ApiKey { get; set; }

        [AIField]
        public string? BaseUrl { get; set; }

        [AIField(IsSensitive = true, EditorUiAlias = "Umb.PropertyEditorUi.TextArea")]
        public string? PrivateKey { get; set; }

        [AIField(IsSensitive = true)]
        public int RotationDays { get; set; }
    }

    private AIEditableModelField GetField(string key)
        => _builder.BuildForType<TestSettings>("test").Fields.Single(f => f.Key == key);

    [Fact]
    public void BuildForType_WithSensitiveString_UsesMaskedEditor()
    {
        // A sensitive credential should never render as a plain text box by default,
        // so it isn't left on screen during screen shares and demos.
        GetField("apiKey").EditorUiAlias.ShouldBe(MaskedTextBox);
    }

    [Fact]
    public void BuildForType_WithNonSensitiveString_UsesTextBox()
    {
        GetField("baseUrl").EditorUiAlias.ShouldBe(TextBox);
    }

    [Fact]
    public void BuildForType_WithSensitiveStringAndExplicitEditor_KeepsExplicitEditor()
    {
        // The explicit alias is the escape hatch: masking a multi-line field would make it
        // unusable, so an author who names an editor always wins over the inferred default.
        GetField("privateKey").EditorUiAlias.ShouldBe("Umb.PropertyEditorUi.TextArea");
    }

    [Fact]
    public void BuildForType_WithSensitiveNonStringType_KeepsTypeSpecificEditor()
    {
        // Masking only substitutes for a text box. A type with its own editor keeps it.
        GetField("rotationDays").EditorUiAlias.ShouldBe("Umb.PropertyEditorUi.Integer");
    }
}
