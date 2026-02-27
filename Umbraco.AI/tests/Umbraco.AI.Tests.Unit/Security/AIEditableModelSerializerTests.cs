using System.Text.Json;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Security;

namespace Umbraco.AI.Tests.Unit.Security;

public class AIEditableModelSerializerTests
{
    private readonly Mock<IAISensitiveFieldProtector> _protectorMock;
    private readonly AIEditableModelSerializer _serializer;

    public AIEditableModelSerializerTests()
    {
        _protectorMock = new Mock<IAISensitiveFieldProtector>();
        _serializer = new AIEditableModelSerializer(_protectorMock.Object);

        // Default setup: protect returns ENC: prefix, unprotect removes it
        _protectorMock
            .Setup(p => p.Protect(It.IsAny<string?>()))
            .Returns<string?>(v => string.IsNullOrEmpty(v) ? v : $"ENC:{v}");

        _protectorMock
            .Setup(p => p.Unprotect(It.IsAny<string?>()))
            .Returns<string?>(v =>
            {
                if (string.IsNullOrEmpty(v)) return v;
                return v.StartsWith("ENC:") ? v[4..] : v;
            });

        _protectorMock
            .Setup(p => p.IsProtected(It.IsAny<string?>()))
            .Returns<string?>(v => !string.IsNullOrEmpty(v) && v.StartsWith("ENC:"));
    }

    #region Serialize

    [Fact]
    public void Serialize_WithNullModel_ReturnsNull()
    {
        // Act
        var result = _serializer.Serialize(null, null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Serialize_WithNoSchema_ReturnsJsonWithoutEncryption()
    {
        // Arrange
        var model = new TestModel { ApiKey = "secret-key", Endpoint = "https://api.example.com" };

        // Act
        var result = _serializer.Serialize(model, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"apiKey\":\"secret-key\"");
        result.ShouldContain("\"endpoint\":\"https://api.example.com\"");
        _protectorMock.Verify(p => p.Protect(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Serialize_WithSchemaHavingNoSensitiveFields_ReturnsJsonWithoutEncryption()
    {
        // Arrange
        var model = new TestModel { ApiKey = "secret-key", Endpoint = "https://api.example.com" };
        var schema = CreateSchema(new AIEditableModelField
        {
            Key = "apiKey",
            Label = "API Key",
            IsSensitive = false
        }, new AIEditableModelField
        {
            Key = "endpoint",
            Label = "Endpoint",
            IsSensitive = false
        });

        // Act
        var result = _serializer.Serialize(model, schema);

        // Assert
        result.ShouldContain("\"apiKey\":\"secret-key\"");
        result.ShouldContain("\"endpoint\":\"https://api.example.com\"");
        _protectorMock.Verify(p => p.Protect(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Serialize_WithSensitiveField_EncryptsOnlySensitiveFields()
    {
        // Arrange
        var model = new TestModel { ApiKey = "secret-key", Endpoint = "https://api.example.com" };
        var schema = CreateSchema(new AIEditableModelField
        {
            Key = "apiKey",
            Label = "API Key",
            IsSensitive = true
        }, new AIEditableModelField
        {
            Key = "endpoint",
            Label = "Endpoint",
            IsSensitive = false
        });

        // Act
        var result = _serializer.Serialize(model, schema);

        // Assert
        result.ShouldContain("\"apiKey\":\"ENC:secret-key\""); // Encrypted
        result.ShouldContain("\"endpoint\":\"https://api.example.com\""); // Not encrypted
        _protectorMock.Verify(p => p.Protect("secret-key"), Times.Once);
    }

    [Fact]
    public void Serialize_WithMultipleSensitiveFields_EncryptsAllSensitiveFields()
    {
        // Arrange
        var model = new AwsModel
        {
            AccessKeyId = "access-key",
            SecretAccessKey = "secret-key",
            Region = "us-east-1"
        };
        var schema = CreateSchema(new AIEditableModelField
        {
            Key = "accessKeyId",
            Label = "Access Key ID",
            IsSensitive = true
        }, new AIEditableModelField
        {
            Key = "secretAccessKey",
            Label = "Secret Access Key",
            IsSensitive = true
        }, new AIEditableModelField
        {
            Key = "region",
            Label = "Region",
            IsSensitive = false
        });

        // Act
        var result = _serializer.Serialize(model, schema);

        // Assert
        result.ShouldContain("\"accessKeyId\":\"ENC:access-key\"");
        result.ShouldContain("\"secretAccessKey\":\"ENC:secret-key\"");
        result.ShouldContain("\"region\":\"us-east-1\"");
        _protectorMock.Verify(p => p.Protect("access-key"), Times.Once);
        _protectorMock.Verify(p => p.Protect("secret-key"), Times.Once);
    }

    [Fact]
    public void Serialize_WithNullSensitiveField_DoesNotEncrypt()
    {
        // Arrange
        var model = new TestModel { ApiKey = null, Endpoint = "https://api.example.com" };
        var schema = CreateSchema(new AIEditableModelField
        {
            Key = "apiKey",
            Label = "API Key",
            IsSensitive = true
        });

        // Act
        var result = _serializer.Serialize(model, schema);

        // Assert
        result.ShouldContain("\"apiKey\":null");
        _protectorMock.Verify(p => p.Protect(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Serialize_WithConfigurationReference_DoesNotEncrypt()
    {
        // Arrange
        var model = new TestModel { ApiKey = "$OpenAI:ApiKey", Endpoint = "https://api.example.com" };
        var schema = CreateSchema(new AIEditableModelField
        {
            Key = "apiKey",
            Label = "API Key",
            IsSensitive = true
        }, new AIEditableModelField
        {
            Key = "endpoint",
            Label = "Endpoint",
            IsSensitive = false
        });

        // Act
        var result = _serializer.Serialize(model, schema);

        // Assert
        result.ShouldContain("\"apiKey\":\"$OpenAI:ApiKey\""); // Not encrypted - kept as-is
        result.ShouldContain("\"endpoint\":\"https://api.example.com\"");
        _protectorMock.Verify(p => p.Protect(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Serialize_WithMixedConfigReferenceAndActualSecret_EncryptsOnlySecret()
    {
        // Arrange
        var model = new AwsModel
        {
            AccessKeyId = "$AWS:AccessKeyId",  // Config reference
            SecretAccessKey = "actual-secret-key",  // Real secret
            Region = "us-east-1"
        };
        var schema = CreateSchema(new AIEditableModelField
        {
            Key = "accessKeyId",
            Label = "Access Key ID",
            IsSensitive = true
        }, new AIEditableModelField
        {
            Key = "secretAccessKey",
            Label = "Secret Access Key",
            IsSensitive = true
        }, new AIEditableModelField
        {
            Key = "region",
            Label = "Region",
            IsSensitive = false
        });

        // Act
        var result = _serializer.Serialize(model, schema);

        // Assert
        result.ShouldContain("\"accessKeyId\":\"$AWS:AccessKeyId\""); // Not encrypted
        result.ShouldContain("\"secretAccessKey\":\"ENC:actual-secret-key\""); // Encrypted
        result.ShouldContain("\"region\":\"us-east-1\"");
        _protectorMock.Verify(p => p.Protect("actual-secret-key"), Times.Once);
        _protectorMock.Verify(p => p.Protect("$AWS:AccessKeyId"), Times.Never);
    }

    [Theory]
    [InlineData("$ConnectionStrings:MyApi")]
    [InlineData("$OpenAI:ApiKey")]
    [InlineData("$Azure:Endpoint")]
    [InlineData("$EnvironmentVariable")]
    public void Serialize_WithVariousConfigReferences_DoesNotEncrypt(string configReference)
    {
        // Arrange
        var model = new TestModel { ApiKey = configReference, Endpoint = "https://api.example.com" };
        var schema = CreateSchema(new AIEditableModelField
        {
            Key = "apiKey",
            Label = "API Key",
            IsSensitive = true
        });

        // Act
        var result = _serializer.Serialize(model, schema);

        // Assert
        result.ShouldContain($"\"apiKey\":\"{configReference}\""); // Not encrypted
        _protectorMock.Verify(p => p.Protect(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Deserialize

    [Fact]
    public void Deserialize_WithNullJson_ReturnsDefault()
    {
        // Act
        var result = (JsonElement)_serializer.Deserialize(null);

        // Assert
        result.ValueKind.ShouldBe(JsonValueKind.Undefined);
    }

    [Fact]
    public void Deserialize_WithEmptyJson_ReturnsDefault()
    {
        // Act
        var result = (JsonElement)_serializer.Deserialize(string.Empty);

        // Assert
        result.ValueKind.ShouldBe(JsonValueKind.Undefined);
    }

    [Fact]
    public void Deserialize_WithNoEncryptedFields_ReturnsOriginalValues()
    {
        // Arrange
        var json = """{"apiKey":"plain-key","endpoint":"https://api.example.com"}""";

        // Act
        var result = (JsonElement)_serializer.Deserialize(json);

        // Assert
        result.GetProperty("apiKey").GetString().ShouldBe("plain-key");
        result.GetProperty("endpoint").GetString().ShouldBe("https://api.example.com");
    }

    [Fact]
    public void Deserialize_WithEncryptedField_DecryptsValue()
    {
        // Arrange
        var json = """{"apiKey":"ENC:secret-key","endpoint":"https://api.example.com"}""";

        // Act
        var result = (JsonElement)_serializer.Deserialize(json);

        // Assert
        result.GetProperty("apiKey").GetString().ShouldBe("secret-key"); // Decrypted
        result.GetProperty("endpoint").GetString().ShouldBe("https://api.example.com"); // Not encrypted, unchanged
        _protectorMock.Verify(p => p.Unprotect("ENC:secret-key"), Times.Once);
    }

    [Fact]
    public void Deserialize_WithMixedEncryptedAndPlainFields_HandlesCorrectly()
    {
        // Arrange
        var json = """{"apiKey":"ENC:encrypted-value","endpoint":"plain-value","name":"also-plain"}""";

        // Act
        var result = (JsonElement)_serializer.Deserialize(json);

        // Assert
        result.GetProperty("apiKey").GetString().ShouldBe("encrypted-value");
        result.GetProperty("endpoint").GetString().ShouldBe("plain-value");
        result.GetProperty("name").GetString().ShouldBe("also-plain");
    }

    [Fact]
    public void Deserialize_WithNonStringValues_PreservesValues()
    {
        // Arrange
        var json = """{"apiKey":"ENC:secret","port":8080,"enabled":true}""";

        // Act
        var result = (JsonElement)_serializer.Deserialize(json);

        // Assert
        result.GetProperty("apiKey").GetString().ShouldBe("secret");
        result.GetProperty("port").GetInt32().ShouldBe(8080);
        result.GetProperty("enabled").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public void Serialize_WithPreviouslyEncryptedConfigReference_MigratesAutomatically()
    {
        // This test verifies backward compatibility with data encrypted before the $ skip optimization.
        // Scenario: Config reference was encrypted in database before this change, now it should
        // decrypt normally on load, then persist unencrypted on save.

        // Arrange - Simulate a config reference that was encrypted with old behavior
        var configReference = "$OpenAI:ApiKey";
        var oldEncryptedJson = """{"apiKey":"ENC:$OpenAI:ApiKey","endpoint":"https://api.example.com"}""";
        var schema = CreateSchema(new AIEditableModelField
        {
            Key = "apiKey",
            Label = "API Key",
            IsSensitive = true
        });

        // Act 1: Load existing encrypted data (simulates reading from database)
        var deserializedElement = (JsonElement)_serializer.Deserialize(oldEncryptedJson);

        // Assert 1: Should decrypt correctly
        deserializedElement.GetProperty("apiKey").GetString().ShouldBe(configReference);
        _protectorMock.Verify(p => p.Unprotect("ENC:$OpenAI:ApiKey"), Times.Once);

        // Act 2: Save it again (simulates update operation)
        var model = new TestModel
        {
            ApiKey = deserializedElement.GetProperty("apiKey").GetString(),
            Endpoint = deserializedElement.GetProperty("endpoint").GetString()
        };
        var reserializedJson = _serializer.Serialize(model, schema);

        // Assert 2: Should NOT re-encrypt (automatic migration to unencrypted)
        reserializedJson.ShouldContain("\"apiKey\":\"$OpenAI:ApiKey\""); // Plain, not encrypted
        reserializedJson.ShouldNotContain("ENC:"); // No encryption prefix
        _protectorMock.Verify(p => p.Protect(It.IsAny<string>()), Times.Never); // Never encrypted on save
    }

    #endregion

    #region Round Trip

    [Fact]
    public void SerializeAndDeserialize_RoundTrip_PreservesValues()
    {
        // Arrange
        var model = new TestModel { ApiKey = "my-secret-api-key", Endpoint = "https://api.example.com" };
        var schema = CreateSchema(new AIEditableModelField
        {
            Key = "apiKey",
            Label = "API Key",
            IsSensitive = true
        }, new AIEditableModelField
        {
            Key = "endpoint",
            Label = "Endpoint",
            IsSensitive = false
        });

        // Act
        var serialized = _serializer.Serialize(model, schema);
        var deserialized = (JsonElement)_serializer.Deserialize(serialized);

        // Assert
        deserialized.GetProperty("apiKey").GetString().ShouldBe("my-secret-api-key");
        deserialized.GetProperty("endpoint").GetString().ShouldBe("https://api.example.com");
    }

    #endregion

    #region Helpers

    private static AIEditableModelSchema CreateSchema(params AIEditableModelField[] fields)
    {
        return new AIEditableModelSchema(typeof(object), fields.ToList());
    }

    private class TestModel
    {
        public string? ApiKey { get; set; }
        public string? Endpoint { get; set; }
    }

    private class AwsModel
    {
        public string? AccessKeyId { get; set; }
        public string? SecretAccessKey { get; set; }
        public string? Region { get; set; }
    }

    #endregion
}
