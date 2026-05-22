using System.ClientModel;
using System.ClientModel.Primitives;
using Umbraco.AI.Core.Providers.Errors;

namespace Umbraco.AI.Tests.Unit.Providers.Errors;

public class ClientModelProviderErrorClassifierTests
{
    private readonly ClientModelProviderErrorClassifier _classifier = new();

    [Fact]
    public void Classify_NonClientResultException_ReturnsNull()
    {
        var result = _classifier.Classify(new InvalidOperationException("not a CRE"));

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(401, AIProviderErrorCategory.Authentication)]
    [InlineData(429, AIProviderErrorCategory.RateLimited)]
    [InlineData(503, AIProviderErrorCategory.Transient)]
    [InlineData(404, AIProviderErrorCategory.NotFound)]
    [InlineData(400, AIProviderErrorCategory.InvalidRequest)]
    public void Classify_ClientResultException_MapsStatusToCategory(int status, AIProviderErrorCategory expected)
    {
        var ex = new ClientResultException("upstream failed", new FakePipelineResponse(status, body: null));

        var result = _classifier.Classify(ex);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(expected);
    }

    [Fact]
    public void Classify_ClientResultException_WithZeroStatus_IsNetworkError()
    {
        // Status == 0 on ClientResultException means the request never reached the server.
        var ex = new ClientResultException("transport failure", response: null);

        var result = _classifier.Classify(ex);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(AIProviderErrorCategory.NetworkError);
    }

    [Fact]
    public void Classify_ClientResultException_ExtractsOpenAIErrorCode()
    {
        // OpenAI-style error envelope: { "error": { "code": "...", ... } }
        var body = """{"error":{"message":"You exceeded your quota","type":"insufficient_quota","code":"insufficient_quota"}}""";
        var ex = new ClientResultException("rate-limit-equivalent", new FakePipelineResponse(429, body));

        var result = _classifier.Classify(ex);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(AIProviderErrorCategory.RateLimited);
        result.ProviderCode.ShouldBe("insufficient_quota");
    }

    [Fact]
    public void Classify_ClientResultException_BodyIsNotJson_StillReturnsStatusCode()
    {
        var ex = new ClientResultException("server error", new FakePipelineResponse(500, "Internal Server Error"));

        var result = _classifier.Classify(ex);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(AIProviderErrorCategory.Transient);
        result.ProviderCode.ShouldBe("500");
    }

    [Fact]
    public void Classify_WrappedClientResultException_WalksInnerChain()
    {
        var inner = new ClientResultException("inner", new FakePipelineResponse(429, body: null));
        var outer = new InvalidOperationException("outer", inner);

        var result = _classifier.Classify(outer);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(AIProviderErrorCategory.RateLimited);
    }

    /// <summary>
    /// Minimal in-memory <see cref="PipelineResponse"/> for driving ClientResultException tests.
    /// </summary>
    private sealed class FakePipelineResponse : PipelineResponse
    {
        private readonly int _status;
        private BinaryData _content;
        private readonly PipelineResponseHeaders _headers = new FakeHeaders();
        private Stream? _stream;

        public FakePipelineResponse(int status, string? body)
        {
            _status = status;
            _content = body is null ? BinaryData.FromBytes([]) : BinaryData.FromString(body);
        }

        public override int Status => _status;
        public override string ReasonPhrase => string.Empty;
        public override Stream? ContentStream
        {
            get => _stream ??= _content.ToStream();
            set => _stream = value;
        }
        public override BinaryData Content => _content;
        protected override PipelineResponseHeaders HeadersCore => _headers;
        public override BinaryData BufferContent(CancellationToken cancellationToken = default) => _content;
        public override ValueTask<BinaryData> BufferContentAsync(CancellationToken cancellationToken = default) => new(_content);
        public override void Dispose() { }

        private sealed class FakeHeaders : PipelineResponseHeaders
        {
            public override IEnumerator<KeyValuePair<string, string>> GetEnumerator() =>
                Enumerable.Empty<KeyValuePair<string, string>>().GetEnumerator();
            public override bool TryGetValue(string name, out string? value) { value = null; return false; }
            public override bool TryGetValues(string name, out IEnumerable<string>? values) { values = null; return false; }
        }
    }
}
