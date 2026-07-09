using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using Umbraco.AI.Core.Providers.Errors;

namespace Umbraco.AI.Tests.Unit.Providers.Errors;

public class ProviderErrorMappingTests
{
    // --- ClientResultException (OpenAI SDK family) ---

    [Theory]
    [InlineData(401, AIProviderErrorCategory.Authentication)]
    [InlineData(429, AIProviderErrorCategory.RateLimited)]
    [InlineData(503, AIProviderErrorCategory.Transient)]
    [InlineData(404, AIProviderErrorCategory.NotFound)]
    [InlineData(400, AIProviderErrorCategory.InvalidRequest)]
    public void FromException_ClientResultException_MapsStatusToCategory(int status, AIProviderErrorCategory expected)
    {
        var ex = new ClientResultException("upstream failed", new FakePipelineResponse(status, body: null));

        var result = ProviderErrorMapping.FromException(ex);

        result.Category.ShouldBe(expected);
    }

    [Fact]
    public void FromException_ClientResultException_WithZeroStatus_IsNetworkError()
    {
        // Status == 0 on ClientResultException means the request never reached the server.
        var ex = new ClientResultException("transport failure", response: null);

        var result = ProviderErrorMapping.FromException(ex);

        result.Category.ShouldBe(AIProviderErrorCategory.NetworkError);
    }

    [Fact]
    public void FromException_ClientResultException_WithZeroStatus_AndUnrecognisedInner_UsesGenericNetworkMessage()
    {
        // No differentiable transport cause in the inner chain - falls back to the original,
        // still-generic network message rather than guessing.
        var ex = new ClientResultException("transport failure", response: null, new Exception("boom"));

        var result = ProviderErrorMapping.FromException(ex);

        result.Category.ShouldBe(AIProviderErrorCategory.NetworkError);
        result.UserMessage.ShouldBe("Couldn't reach the AI service. Check your connection and try again.");
    }

    [Fact]
    public void FromException_ClientResultException_WrappingConnectionRefused_DifferentiatesMessage()
    {
        var socketEx = new SocketException((int)SocketError.ConnectionRefused);
        var ex = new ClientResultException("transport failure", response: null, socketEx);

        var result = ProviderErrorMapping.FromException(ex);

        result.Category.ShouldBe(AIProviderErrorCategory.NetworkError);
        result.ProviderCode.ShouldBe("connection");
        result.UserMessage.ShouldNotBe("Couldn't reach the AI service. Check your connection and try again.");
    }

    [Fact]
    public void FromException_ClientResultException_WrappingHostNotFound_IsDnsMessage()
    {
        var socketEx = new SocketException((int)SocketError.HostNotFound);
        var ex = new ClientResultException("transport failure", response: null, socketEx);

        var result = ProviderErrorMapping.FromException(ex);

        result.Category.ShouldBe(AIProviderErrorCategory.NetworkError);
        result.ProviderCode.ShouldBe("dns");
    }

    [Fact]
    public void FromException_ClientResultException_WrappingAuthenticationFailure_IsTlsMessage()
    {
        var authEx = new AuthenticationException("The remote certificate is invalid.");
        var ex = new ClientResultException("transport failure", response: null, authEx);

        var result = ProviderErrorMapping.FromException(ex);

        result.Category.ShouldBe(AIProviderErrorCategory.NetworkError);
        result.ProviderCode.ShouldBe("tls");
    }

    [Fact]
    public void FromException_ClientResultException_ExtractsOpenAIErrorCode()
    {
        // OpenAI-style error envelope: { "error": { "code": "...", ... } }
        var body = """{"error":{"message":"You exceeded your quota","type":"insufficient_quota","code":"insufficient_quota"}}""";
        var ex = new ClientResultException("rate-limit-equivalent", new FakePipelineResponse(429, body));

        var result = ProviderErrorMapping.FromException(ex);

        result.Category.ShouldBe(AIProviderErrorCategory.RateLimited);
        result.ProviderCode.ShouldBe("insufficient_quota");
    }

    [Fact]
    public void FromException_ClientResultException_BodyIsNotJson_StillReturnsStatusCode()
    {
        var ex = new ClientResultException("server error", new FakePipelineResponse(500, "Internal Server Error"));

        var result = ProviderErrorMapping.FromException(ex);

        result.Category.ShouldBe(AIProviderErrorCategory.Transient);
        result.ProviderCode.ShouldBe("500");
    }

    [Fact]
    public void FromException_WrappedClientResultException_WalksInnerChain()
    {
        var inner = new ClientResultException("inner", new FakePipelineResponse(429, body: null));
        var outer = new InvalidOperationException("outer", inner);

        var result = ProviderErrorMapping.FromException(outer);

        result.Category.ShouldBe(AIProviderErrorCategory.RateLimited);
    }

    // --- BCL transport types ---

    [Fact]
    public void FromException_OperationCancelled_ReturnsCancelled()
    {
        var result = ProviderErrorMapping.FromException(new OperationCanceledException("internal-only message"));

        result.Category.ShouldBe(AIProviderErrorCategory.Cancelled);
        result.UserMessage.ShouldNotBe(result.RawMessage);   // friendly message, not raw text
        result.RawMessage.ShouldContain("internal-only message");
    }

    [Fact]
    public void FromException_TaskCanceled_ReturnsCancelled()
    {
        // TaskCanceledException derives from OperationCanceledException — covered by the same case.
        var result = ProviderErrorMapping.FromException(new TaskCanceledException("token cancelled"));

        result.Category.ShouldBe(AIProviderErrorCategory.Cancelled);
    }

    [Fact]
    public void FromException_TaskCanceled_WrappingTimeout_ReturnsTransient()
    {
        // HttpClient reports its own request timeout as a TaskCanceledException wrapping a
        // TimeoutException — this must be classified as a timeout, not a plain cancellation.
        var timeout = new TimeoutException("The request timed out.");
        var ex = new TaskCanceledException("A task was canceled.", timeout);

        var result = ProviderErrorMapping.FromException(ex);

        result.Category.ShouldBe(AIProviderErrorCategory.Transient);
        result.ProviderCode.ShouldBe("timeout");
    }

    [Fact]
    public void FromException_Timeout_ReturnsTransient()
    {
        var result = ProviderErrorMapping.FromException(new TimeoutException("upstream timeout"));

        result.Category.ShouldBe(AIProviderErrorCategory.Transient);
        result.ProviderCode.ShouldBe("timeout");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, AIProviderErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.Forbidden, AIProviderErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.NotFound, AIProviderErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests, AIProviderErrorCategory.RateLimited)]
    [InlineData(HttpStatusCode.BadGateway, AIProviderErrorCategory.Transient)]
    [InlineData(HttpStatusCode.ServiceUnavailable, AIProviderErrorCategory.Transient)]
    [InlineData(HttpStatusCode.BadRequest, AIProviderErrorCategory.InvalidRequest)]
    public void FromException_HttpRequestException_WithStatus_MapsToCategory(HttpStatusCode status, AIProviderErrorCategory expected)
    {
        var ex = new HttpRequestException("upstream failed", inner: null, status);

        var result = ProviderErrorMapping.FromException(ex);

        result.Category.ShouldBe(expected);
    }

    [Fact]
    public void FromException_HttpRequestException_WithoutStatus_IsNetworkError()
    {
        var result = ProviderErrorMapping.FromException(new HttpRequestException("connection refused"));

        result.Category.ShouldBe(AIProviderErrorCategory.NetworkError);
    }

    [Fact]
    public void FromException_HttpRequestException_WithNameResolutionError_IsDnsMessage()
    {
        var ex = new HttpRequestException(HttpRequestError.NameResolutionError, "no such host", inner: null, statusCode: null);

        var result = ProviderErrorMapping.FromException(ex);

        result.Category.ShouldBe(AIProviderErrorCategory.NetworkError);
        result.ProviderCode.ShouldBe("dns");
    }

    [Fact]
    public void FromException_HttpRequestException_WithSecureConnectionError_IsTlsMessage()
    {
        var ex = new HttpRequestException(HttpRequestError.SecureConnectionError, "handshake failed", inner: null, statusCode: null);

        var result = ProviderErrorMapping.FromException(ex);

        result.Category.ShouldBe(AIProviderErrorCategory.NetworkError);
        result.ProviderCode.ShouldBe("tls");
    }

    [Fact]
    public void FromException_WrappedHttpRequestException_WalksInnerChain()
    {
        // The OpenAI SDK and others wrap HttpRequestException in transport-layer exceptions.
        var inner = new HttpRequestException("network", inner: null, HttpStatusCode.ServiceUnavailable);
        var outer = new InvalidOperationException("wrapped", inner);

        var result = ProviderErrorMapping.FromException(outer);

        result.Category.ShouldBe(AIProviderErrorCategory.Transient);
    }

    [Fact]
    public void FromException_UnrecognisedException_ReturnsUnknown()
    {
        // Anything outside the transport types the default mapping handles becomes Unknown,
        // never leaking the raw exception text as the user message.
        var result = ProviderErrorMapping.FromException(new InvalidOperationException("internal-only detail"));

        result.Category.ShouldBe(AIProviderErrorCategory.Unknown);
        result.UserMessage.ShouldNotContain("internal-only detail");
        result.RawMessage.ShouldContain("internal-only detail");
    }

    /// <summary>
    /// Minimal in-memory <see cref="PipelineResponse"/> for driving ClientResultException tests.
    /// </summary>
    private sealed class FakePipelineResponse : PipelineResponse
    {
        private readonly int _status;
        private readonly BinaryData _content;
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
