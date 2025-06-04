// Copyright © devsko 2025. All rights reserved.
// Licensed under the MIT license.

namespace Spectre.Builder;

public interface IResource
{
    string Name { get; }
    bool IsAvailable { get; }
    DateTimeOffset? LastUpdated { get; }
}

public class Resource(DateTimeOffset? lastUpdated) : IResource
{
    private object? _value;

    public object? Value => _value;

    public string Name => string.Empty;

    public bool IsAvailable => true;

    public DateTimeOffset? LastUpdated => lastUpdated;

    public void Set(object? value)
    {
        _value = value;
    }
}

public class FileResource(string path) : IResource
{
    private readonly FileInfo _file = new(path);

    public string Name => _file.Name;

    public string Path => _file.FullName;

    public bool IsAvailable
    {
        get
        {
            _file.Refresh();
            return _file.Exists;
        }
    }

    public DateTimeOffset? LastUpdated
    {
        get
        {
            _file.Refresh();
            if (!_file.Exists)
            {
                return null;
            }

            DateTime created = _file.CreationTimeUtc;
            DateTime lastWrite = _file.LastWriteTimeUtc;

            return created > lastWrite ? created : lastWrite;
        }
    }

    public void Delete()
    {
        _file.Delete();
    }

    public PreliminaryFileStream OpenCreate(int bufferSize, DateTime timestamp)
    {
        if (Directory.Exists(Path))
        {
            throw new InvalidOperationException();
        }

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);

        return new PreliminaryFileStream(Path, bufferSize, timestamp);
    }

    public FileStream OpenRead(int bufferSize)
    {
        return File.Open(Path, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read, BufferSize = bufferSize });
    }
}

public class DirectoryResource(string path) : IResource
{
    public const string UpdateFileName = "_update.dir";

    private readonly DirectoryInfo _directory = new(path);
    private readonly FileInfo _dirUpdateFile = new(System.IO.Path.Combine(path, UpdateFileName));

    public string Name => _directory.Name;

    public string Path => _directory.FullName;

    public bool IsAvailable
    {
        get
        {
            _directory.Refresh();
            return _directory.Exists;
        }
    }

    public DateTimeOffset? LastUpdated
    {
        get
        {
            _dirUpdateFile.Refresh();
            return _dirUpdateFile.Exists ? _dirUpdateFile.LastWriteTimeUtc : null;
        }
    }

    public FileInfo GetFile(string name)
        => new FileInfo(System.IO.Path.Combine(_directory.FullName, name));

    public IEnumerable<FileInfo> GetFiles(string searchPattern)
        => _directory.EnumerateFiles(searchPattern);

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
