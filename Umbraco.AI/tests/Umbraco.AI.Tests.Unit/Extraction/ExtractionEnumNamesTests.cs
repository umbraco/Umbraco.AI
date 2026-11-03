#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability surface

using Umbraco.AI.Core.Extraction;

namespace Umbraco.AI.Tests.Unit.Extraction;

public class ExtractionEnumNamesTests
{
    [Theory]
    [InlineData("seconds")]
    [InlineData("HOURS")]
    [InlineData(" Hours ")]
    public void TryParse_Returns_True_For_Member_Names_Ignoring_Case_And_Surrounding_Whitespace(string input)
    {
        ExtractionEnumNames.TryParse<AIDurationUnit>(input, out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData("seconds", AIDurationUnit.Seconds)]
    [InlineData("HOURS", AIDurationUnit.Hours)]
    [InlineData(" Hours ", AIDurationUnit.Hours)]
    public void TryParse_Outputs_The_Matching_Member(string input, AIDurationUnit expected)
    {
        ExtractionEnumNames.TryParse<AIDurationUnit>(input, out var result);
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("3")]
    [InlineData("seconds,days")]
    [InlineData("fortnights")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_Rejects_Numbers_Flag_Lists_Unknown_Names_And_Blanks(string? input)
    {
        ExtractionEnumNames.TryParse<AIDurationUnit>(input, out _).ShouldBeFalse();
    }
}
