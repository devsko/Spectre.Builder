// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a downloadable resource accessible via HTTP.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DownloadableResource"/> class.
/// </remarks>
/// <param name="uri">The URI of the downloadable resource.</param>
public class DownloadableResource(Uri uri) : IResource
{
    private readonly HttpClient _client = new();
    private bool _availabilityChecked;

    /// <summary>
    /// Gets the URI of the downloadable resource.
    /// </summary>
    public Uri Uri { get; private set; } = uri;

    /// <inheritdoc/>
    public bool IsRequired { get; init; } = true;

    /// <inheritdoc/>
    public bool IsAvailable { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? LastUpdated { get; private set; }

    /// <summary>
    /// Gets the length of the resource in bytes, or null if unknown.
    /// </summary>
    public long? Length { get; private set; }

    /// <inheritdoc/>
    public string Name => Uri.ToString();

    /// <summary>
    /// Gets the URI of the downloadable resource.
    /// </summary>
    /// <inheritdoc/>
    async Task IResource.DetermineAvailabilityAsync(CancellationToken cancellationToken)
    {
        if (_availabilityChecked)
        {
            return;
        }

        _availabilityChecked = true;

        try
        {
            using HttpResponseMessage response = await _client.GetAsync(Uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                IsAvailable = true;
                LastUpdated = response.Content.Headers.LastModified;
                Length = response.Content.Headers.ContentLength;

                return;
            }
        }
        catch (HttpRequestException)
        { }
    }

    /// <summary>
    /// Asynchronously downloads the resource as a stream.
    /// </summary>
    /// <param name="progress">An optional action to report download progress in bytes.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous download operation. The value of the TResult parameter contains the stream of the downloaded resource.</returns>
    public async Task<Stream> DownloadAsync(Action<int>? progress = null, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _client.GetAsync(Uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        if (progress is not null)
        {
            stream = new ProgressStream(stream, progress);
        }

        return stream;
    }
}
