// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a preliminary directory that can be persisted or discarded.
/// </summary>
public sealed class PreliminaryDirectory : IDisposable
{
    private readonly string _path;
    private readonly DateTime _updateTime;
    private string? _tempPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreliminaryDirectory"/> class.
    /// </summary>
    /// <param name="path">The target directory path to persist to.</param>
    /// <param name="updateTime">The update time to set for the persisted directory.</param>
    public PreliminaryDirectory(string path, DateTime updateTime)
    {
        _path = path;
        _updateTime = updateTime;
        _tempPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!, $"{System.IO.Path.GetFileName(path)}_{System.IO.Path.GetRandomFileName()}");
        Directory.CreateDirectory(_tempPath);
    }

    /// <summary>
    /// Finalizer for the <see cref="PreliminaryDirectory"/> class.
    /// Ensures that resources are released if <see cref="Dispose()"/> was not called.
    /// </summary>
    ~PreliminaryDirectory()
    {
        Dispose(false);
    }

    /// <summary>
    /// Gets the path of the preliminary directory.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the directory has already been disposed.</exception>
    public string Path => _tempPath ?? throw new InvalidOperationException();

    /// <summary>
    /// Persists the preliminary directory to the target path, replacing any existing directory.
    /// </summary>
    public void Persist()
    {
        using (FileStream updateFile = File.Create(System.IO.Path.Combine(Path, DirectoryResource.UpdateFileName)))
        {
            File.SetLastWriteTimeUtc(updateFile.SafeFileHandle, _updateTime);
        }

        if (Directory.Exists(_path))
        {
            Directory.Delete(_path, true);
        }
        Directory.Move(Path, _path);
    }

    private void Dispose(bool _)
    {
        if (Directory.Exists(Path))
        {
            try
            {
                Directory.Delete(Path, true);
            }
            catch { }
        }

        _tempPath = null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
