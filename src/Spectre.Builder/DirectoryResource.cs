// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a directory resource with utility methods for file and directory management.
/// </summary>
public class DirectoryResource(string path) : IResource
{
    /// <summary>
    /// The name of the file used to track last update timestamp.
    /// </summary>
    public const string UpdateFileName = "_update.dir";

    private readonly DirectoryInfo _directory = new(path);
    private readonly FileInfo _dirUpdateFile = new(System.IO.Path.Combine(path, UpdateFileName));

    /// <inheritdoc/>
    public bool IsRequired { get; init; } = true;

    /// <inheritdoc/>
    public string Name => _directory.Name;

    /// <summary>
    /// Gets the full path of the directory resource.
    /// </summary>
    public string Path => _directory.FullName;

    /// <inheritdoc/>
    public bool IsAvailable
    {
        get
        {
            return _directory.Exists;
        }
    }

    /// <summary>
    /// Gets the last updated timestamp of the directory, based on the update file.
    /// </summary>
    public DateTimeOffset? LastUpdated
    {
        get
        {
            return _dirUpdateFile.Exists ? _dirUpdateFile.LastWriteTimeUtc : null;
        }
    }

    /// <inheritdoc/>
    Task IResource.DetermineAvailabilityAsync(CancellationToken cancellationToken)
    {
        _directory.Refresh();
        _dirUpdateFile.Refresh();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a <see cref="FileInfo"/> for a file with the specified name in the directory.
    /// </summary>
    /// <param name="name">The name of the file.</param>
    /// <returns>A <see cref="FileInfo"/> object for the specified file.</returns>
    public FileInfo GetFile(string name)
        => new FileInfo(System.IO.Path.Combine(_directory.FullName, name));

    /// <summary>
    /// Gets an enumerable collection of files in the directory that match the specified search pattern.
    /// </summary>
    /// <param name="searchPattern">The search string to match against the names of files.</param>
    /// <returns>An enumerable collection of <see cref="FileInfo"/> objects.</returns>
    public IEnumerable<FileInfo> GetFiles(string searchPattern)
        => _directory.EnumerateFiles(searchPattern);

    /// <summary>
    /// Creates a new preliminary directory at the resource path with the specified timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp to use for the update file.</param>
    /// <returns>A <see cref="PreliminaryDirectory"/> representing the created directory.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a file exists at the directory path.</exception>
    public PreliminaryDirectory Create(DateTime timestamp)
    {
        if (File.Exists(Path))
        {
            throw new InvalidOperationException();
        }

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);

        return new PreliminaryDirectory(Path, timestamp);
    }
}
