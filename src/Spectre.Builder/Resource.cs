// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Spectre.Builder;

/// <summary>
/// Represents a generic resource with a value of type <see cref="T"/> and metadata such as last update time.
/// <typeparamref name="T">The type of the value represented by this resource.</typeparamref>
/// </summary>
public class Resource<T>(DateTimeOffset? lastUpdated) : IResource
{
    private T? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Resource{T}"/> class using the latest <see cref="IResource.LastUpdated"/>
    /// value from the specified dependencies.
    /// </summary>
    /// <param name="dependsOn">A span of resources that this resource depends on. The most recent <see cref="IResource.LastUpdated"/> value among them is used.</param>
    public Resource(params ReadOnlySpan<IResource> dependsOn)
        : this(GetLastUpdated(dependsOn))
    { }

    private static DateTimeOffset? GetLastUpdated(ReadOnlySpan<IResource> dependsOn)
    {
        DateTimeOffset? lastUpdated = null;
        foreach (IResource resource in dependsOn)
        {
            if (resource.LastUpdated is DateTimeOffset date && (lastUpdated is null || date > lastUpdated.Value))
            {
                lastUpdated = date;
            }
        }

        return lastUpdated;
    }

    /// <summary>
    /// Gets the value of the resource.
    /// </summary>
    public T? Value => _value;

    /// <inheritdoc/>
    public string Name => typeof(T).Name;

    /// <inheritdoc/>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsAvailable => _value is not null;

    /// <inheritdoc/>
    public DateTimeOffset? LastUpdated => lastUpdated;

    /// <summary>
    /// Sets the value of the resource.
    /// </summary>
    /// <param name="value">The value to set.</param>
    public void Set(T? value)
    {
        _value = value;
    }
}
