// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// A stream wrapper that reports progress on read and write operations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProgressStream"/> class.
/// </remarks>
/// <param name="baseStream">The underlying stream to wrap.</param>
/// <param name="progress">The progress reporter to use.</param>
public sealed class ProgressStream(Stream baseStream, IProgress<int> progress) : Stream
{

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressStream"/> class.
    /// </summary>
    /// <param name="baseStream">The underlying stream to wrap.</param>
    /// <param name="progress">The action to invoke with the number of bytes read/written.</param>
    public ProgressStream(Stream baseStream, Action<int> progress)
        : this(baseStream, new Progress<int>(progress))
    { }

    /// <inheritdoc/>
    public override bool CanRead => baseStream.CanRead;

    /// <inheritdoc/>
    public override bool CanSeek => baseStream.CanSeek;

    /// <inheritdoc/>
    public override bool CanWrite => baseStream.CanWrite;

    /// <inheritdoc/>
    public override long Length => baseStream.Length;

    /// <inheritdoc/>
    public override long Position
    {
        get => baseStream.Position;
        set => baseStream.Position = value;
    }

    /// <inheritdoc/>
    public override void Flush() => baseStream.Flush();

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => baseStream.Seek(offset, origin);

    /// <inheritdoc/>
    public override void SetLength(long value) => baseStream.SetLength(value);

    /// <inheritdoc/>
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int bytesRead = await baseStream.ReadAsync(buffer, cancellationToken);
        progress.Report(bytesRead);

        return bytesRead;
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesRead = baseStream.Read(buffer, offset, count);
        progress.Report(bytesRead);

        return bytesRead;
    }

    /// <inheritdoc/>
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await baseStream.WriteAsync(buffer, cancellationToken);
        progress.Report(buffer.Length);
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        baseStream.Write(buffer, offset, count);
        progress.Report(count);
    }

    /// <inheritdoc/>
    public override ValueTask DisposeAsync() => baseStream.DisposeAsync();
}
