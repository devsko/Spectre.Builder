// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a downloadable resource accessible via HTTP.
/// </summary>
public class DownloadableResource : IResource
{
    private readonly HttpClient _client = new();

    /// <summary>
    /// Gets the URI of the downloadable resource.
    /// </summary>
    public Uri Uri { get; private set; }

    /// <inheritdoc/>
    public bool IsAvailable { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? LastUpdated { get; private set; }

    /// <summary>
    /// Gets the length of the resource in bytes, or null if unknown.
    /// </summary>
    public long? Length { get; private set; }

    private DownloadableResource(Uri uri, HttpClient client)
    {
        Uri = uri;
        _client = client;
    }

    /// <summary>
    /// Asynchronously creates a <see cref="DownloadableResource"/> for the specified URI.
    /// </summary>
    /// <param name="uri">The URI of the resource to download.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous creation operation. The value of the TResult parameter contains the created <see cref="DownloadableResource"/>.</returns>
    public static async Task<DownloadableResource> CreateAsync(Uri uri, CancellationToken cancellationToken)
    {
        HttpClient client = new();
        bool isAvailable = false;
        DateTimeOffset? lastUpdated = null;
        long? length = null;

        try
        {
            HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                isAvailable = true;
                lastUpdated = response.Content.Headers.LastModified;
                length = response.Content.Headers.ContentLength;
            }
        }
        catch (HttpRequestException)
        { }

        return new DownloadableResource(uri, client) { IsAvailable = isAvailable, LastUpdated = lastUpdated, Length = length };
    }

    /// <inheritdoc/>
    public string Name => Uri.ToString();

    /// <summary>
    /// Asynchronously downloads the resource as a stream.
    /// </summary>
    /// <param name="progress">An optional action to report download progress in bytes.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous download operation. The value of the TResult parameter contains the stream of the downloaded resource.</returns>
    public async Task<Stream> DownloadAsync(Action<int>? progress = null, CancellationToken cancellationToken = default)
    {
        Stream stream = await (await _client.GetAsync(Uri, cancellationToken)).Content.ReadAsStreamAsync(cancellationToken);
        if (progress is not null)
        {
            stream = new ProgressStream(stream, progress);
        }

        return stream;
    }
}
