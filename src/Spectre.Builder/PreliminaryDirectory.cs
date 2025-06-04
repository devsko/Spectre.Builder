// Copyright © devsko 2025. All rights reserved.
// Licensed under the MIT license.

namespace Spectre.Builder;

public sealed class PreliminaryDirectory : IDisposable
{
    private readonly string _path;
    private readonly DateTime _updateTime;
    private string? _tempPath;

    public PreliminaryDirectory(string path, DateTime updateTime)
    {
        _path = path;
        _updateTime = updateTime;
        _tempPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!, $"{System.IO.Path.GetFileName(path)}_{System.IO.Path.GetRandomFileName()}");
        Directory.CreateDirectory(_tempPath);
    }

    ~PreliminaryDirectory()
    {
        Dispose(false);
    }

    public string Path => _tempPath ?? throw new InvalidOperationException();

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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
