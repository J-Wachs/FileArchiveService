namespace FileArchive.Utils;

/// <summary>
/// This class is used to wrap the file stram that is returned to the web server
/// in order for the stream to be closed after the download.
/// </summary>
public sealed class StreamHelper : IDisposable
{
    private Stream? stream = null;

    /// <summary>
    /// Stores the steam in this helper.
    /// </summary>
    /// <param name="theStream">The stream to manage</param>
    /// <returns></returns>
    public Stream SetFileStream(Stream theStream)
    {
        stream = theStream;
        return stream;
    }

    /// <summary>
    /// Dispose of the stream.
    /// </summary>
    public void Dispose()
    {
        stream?.Dispose();
    }
}
