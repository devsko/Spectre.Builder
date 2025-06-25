// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a resource with a name, availability status, and last updated timestamp.
/// </summary>
public interface IResource
{
    /// <summary>
    /// Gets the name of the resource.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a value indicating whether the resource is required.
    /// </summary>
    bool IsRequired { get; }

    /// <summary>
    /// Gets a value indicating whether the resource is available.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets the date and time when the resource was last updated, or null if unknown.
    /// </summary>
    DateTimeOffset? LastUpdated { get; }

    /// <summary>
    /// Determines the availability of the resource and when it was last updated asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DetermineAvailabilityAsync(CancellationToken cancellationToken);
}
