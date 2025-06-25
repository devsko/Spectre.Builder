// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// A resource used as output of calculation steps that in the end results in the provided <see cref="IResource"/>.
/// </summary>
/// <param name="targetResource">The resource that is output of a step executed some time after the step uses this resource.</param>
public class CalculationResource(IResource targetResource) : IResource
{
    /// <inheritdoc/>
    public string Name => string.Empty;

    /// <inheritdoc/>
    public bool IsRequired => true;

    /// <inheritdoc/>
    public bool IsAvailable => true;

    /// <inheritdoc/>
    public DateTimeOffset? LastUpdated => targetResource.LastUpdated;

    Task IResource.DetermineAvailabilityAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
