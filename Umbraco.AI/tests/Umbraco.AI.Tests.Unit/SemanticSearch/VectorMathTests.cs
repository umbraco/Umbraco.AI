using Shouldly;
using Umbraco.AI.Core.SemanticSearch;
using Xunit;

namespace Umbraco.AI.Tests.Unit.SemanticSearch;

public class VectorMathTests
{
    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        // Arrange
        float[] a = [1f, 2f, 3f];
        float[] b = [1f, 2f, 3f];

        // Act
        var result = VectorMath.CosineSimilarity(a, b);

        // Assert
        result.ShouldBe(1f, tolerance: 0.0001f);
    }

    [Fact]
    public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        // Arrange
        float[] a = [1f, 0f];
        float[] b = [0f, 1f];

        // Act
        var result = VectorMath.CosineSimilarity(a, b);

        // Assert
        result.ShouldBe(0f, tolerance: 0.0001f);
    }

    [Fact]
    public void CosineSimilarity_OppositeVectors_ReturnsNegativeOne()
    {
        // Arrange
        float[] a = [1f, 2f, 3f];
        float[] b = [-1f, -2f, -3f];

        // Act
        var result = VectorMath.CosineSimilarity(a, b);

        // Assert
        result.ShouldBe(-1f, tolerance: 0.0001f);
    }

    [Fact]
    public void CosineSimilarity_DifferentLengthVectors_ReturnsZero()
    {
        // Arrange
        float[] a = [1f, 2f];
        float[] b = [1f, 2f, 3f];

        // Act
        var result = VectorMath.CosineSimilarity(a, b);

        // Assert
        result.ShouldBe(0f);
    }

    [Fact]
    public void CosineSimilarity_EmptyVectors_ReturnsZero()
    {
        // Arrange
        float[] a = [];
        float[] b = [];

        // Act
        var result = VectorMath.CosineSimilarity(a, b);

        // Assert
        result.ShouldBe(0f);
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesValues()
    {
        // Arrange
        float[] original = [0.1f, 0.2f, -0.3f, 0.456789f, 1.0f];

        // Act
        var bytes = VectorMath.SerializeVector(original);
        var deserialized = VectorMath.DeserializeVector(bytes);

        // Assert
        deserialized.Length.ShouldBe(original.Length);
        for (var i = 0; i < original.Length; i++)
        {
            deserialized[i].ShouldBe(original[i]);
        }
    }

    [Fact]
    public void SerializeVector_ProducesCorrectByteLength()
    {
        // Arrange
        float[] vector = [1f, 2f, 3f];

        // Act
        var bytes = VectorMath.SerializeVector(vector);

        // Assert - each float is 4 bytes
        bytes.Length.ShouldBe(vector.Length * sizeof(float));
    }
}
