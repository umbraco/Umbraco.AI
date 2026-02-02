namespace Umbraco.Ai.Core.Tools.Web;

/// <summary>
/// A stream wrapper that enforces a maximum number of bytes that can be read.
/// Throws <see cref="InvalidOperationException"/> when the limit is exceeded.
/// </summary>
public class LimitedStream : Stream
{
    private readonly Stream _innerStream;
    private readonly long _maxBytes;
    private long _bytesRead;

    /// <summary>
    /// Initializes a new instance of the <see cref="LimitedStream"/> class.
    /// </summary>
    /// <param name="innerStream">The inner stream to wrap.</param>
    /// <param name="maxBytes">The maximum number of bytes that can be read.</param>
    public LimitedStream(Stream innerStream, long maxBytes)
    {
        _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        _maxBytes = maxBytes;
        _bytesRead = 0;
    }

    /// <inheritdoc />
    public override bool CanRead => _innerStream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => _innerStream.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => _innerStream.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => _innerStream.Position;
        set => throw new NotSupportedException("Setting position is not supported");
    }

    /// <inheritdoc />
    public override void Flush() => _innerStream.Flush();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _innerStream.Read(buffer, offset, count);
        _bytesRead += bytesRead;

        if (_bytesRead > _maxBytes)
            throw new InvalidOperationException($"Response size exceeds maximum allowed size of {_maxBytes} bytes");

        return bytesRead;
    }

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        _bytesRead += bytesRead;

        if (_bytesRead > _maxBytes)
            throw new InvalidOperationException($"Response size exceeds maximum allowed size of {_maxBytes} bytes");

        return bytesRead;
    }

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, cancellationToken);
        _bytesRead += bytesRead;

        if (_bytesRead > _maxBytes)
            throw new InvalidOperationException($"Response size exceeds maximum allowed size of {_maxBytes} bytes");

        return bytesRead;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException("Setting length is not supported");

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("Writing is not supported");

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerStream.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await _innerStream.DisposeAsync();
        await base.DisposeAsync();
    }
}
