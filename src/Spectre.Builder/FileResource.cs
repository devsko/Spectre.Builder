// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a file-based resource.
/// </summary>
public class FileResource(string path) : IResource
{
    private readonly FileInfo _file = new(path);

    /// <inheritdoc/>
    public bool IsRequired { get; init; } = true;

    /// <inheritdoc/>
    public string Name => _file.Name;

    /// <summary>
    /// Gets the full path of the file resource.
    /// </summary>
    public string Path => _file.FullName;

    /// <inheritdoc/>
    public bool IsAvailable => _file.Exists;

    /// <inheritdoc/>
    public DateTimeOffset? LastUpdated
    {
        get
        {
            if (!_file.Exists)
            {
                return null;
            }

            DateTime created = _file.CreationTimeUtc;
            DateTime lastWrite = _file.LastWriteTimeUtc;

            return created > lastWrite ? created : lastWrite;
        }
    }

    /// <inheritdoc/>
    Task IResource.DetermineAvailabilityAsync(CancellationToken cancellationToken)
    {
        _file.Refresh();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Deletes the file resource.
    /// </summary>
    public void Delete()
    {
        _file.Delete();
    }

    /// <summary>
    /// Opens the file for creation with the specified buffer size and timestamp.
    /// </summary>
    /// <param name="bufferSize">The buffer size to use for the file stream.</param>
    /// <param name="timestamp">The timestamp to associate with the file.</param>
    /// <returns>A <see cref="PreliminaryFileStream"/> for writing to the file.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the path is a directory.</exception>
    public PreliminaryFileStream OpenCreate(int bufferSize, DateTime timestamp)
    {
        if (Directory.Exists(Path))
        {
            throw new InvalidOperationException();
        }

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);

        return new PreliminaryFileStream(Path, bufferSize, timestamp);
    }

    /// <summary>
    /// Opens the file for reading with the specified buffer size.
    /// </summary>
    /// <param name="bufferSize">The buffer size to use for the file stream.</param>
    /// <returns>A <see cref="FileStream"/> for reading from the file.</returns>
    public FileStream OpenRead(int bufferSize)
    {
        return File.Open(Path, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read, BufferSize = bufferSize });
    }
}
