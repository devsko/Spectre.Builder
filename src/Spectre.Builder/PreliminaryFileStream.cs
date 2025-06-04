// Copyright (c) devsko. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Spectre.Builder;

/// <summary>
/// Represents a file stream for preliminary file operations, allowing atomic persistence and cleanup.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PreliminaryFileStream"/> class.
/// </remarks>
/// <param name="path">The target file path.</param>
/// <param name="bufferSize">The buffer size to use for the stream.</param>
/// <param name="updateTime">The last write time to set when persisting the file.</param>
public sealed class PreliminaryFileStream(string path, int bufferSize, DateTime updateTime) : FileStream(
        Path.Combine(Path.GetDirectoryName(path)!, $"{Path.GetFileName(path)}_{Path.GetRandomFileName()}"),
        new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write, Share = FileShare.Read, BufferSize = bufferSize })
{
    /// <summary>
    /// Persists the preliminary file to the target path and setting the last write time.
    /// </summary>
    /// <returns>A task that represents the asynchronous persist operation.</returns>
    public async Task PersistAsync()
    {
        await FlushAsync();
        File.SetLastWriteTimeUtc(SafeFileHandle, updateTime);
        SafeFileHandle.Dispose();
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        File.Move(Name, path);
        await DisposeAsync();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        try
        {
            base.Dispose(disposing);
        }
        finally
        {
            if (File.Exists(Name))
            {
                try
                {
                    File.Delete(Name);
                }
                catch { }
            }
        }
    }
}
