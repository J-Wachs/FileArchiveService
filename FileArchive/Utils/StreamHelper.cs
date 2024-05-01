namespace FileArchive.Utils;

/// <summary>
/// This class is used to wrap the file stram that is returned to the web server
/// in order for the stream to be closed after the download.
/// </summary>
public sealed class StreamHelper : IDisposable
{
    private Stream? stream = null;

    public Stream CreateFileStream(Stream theStream)
    {
        stream = theStream;
        return stream;
    }

    public void Dispose()
    {
        stream?.Dispose();
    }
}
