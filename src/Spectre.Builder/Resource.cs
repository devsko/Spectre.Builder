// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a generic resource with a value and metadata such as last update time.
/// </summary>
public class Resource(DateTimeOffset? lastUpdated) : IResource
{
    private object? _value;

    /// <summary>
    /// Gets the value of the resource.
    /// </summary>
    public object? Value => _value;

    /// <inheritdoc/>
    public string Name => string.Empty;

    /// <inheritdoc/>
    public bool IsAvailable => true;

    /// <inheritdoc/>
    public DateTimeOffset? LastUpdated => lastUpdated;

    /// <summary>
    /// Sets the value of the resource.
    /// </summary>
    /// <param name="value">The value to set.</param>
    public void Set(object? value)
    {
        _value = value;
    }
}
