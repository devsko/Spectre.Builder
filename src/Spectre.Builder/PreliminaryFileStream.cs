// Copyright © devsko 2025. All rights reserved.
// Licensed under the MIT license.

namespace Spectre.Builder;

public sealed class PreliminaryFileStream(string path, int bufferSize, DateTime updateTime)
    : FileStream(
        Path.Combine(Path.GetDirectoryName(path)!, $"{Path.GetFileName(path)}_{Path.GetRandomFileName()}"),
        new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write, Share = FileShare.Read, BufferSize = bufferSize })
{
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
