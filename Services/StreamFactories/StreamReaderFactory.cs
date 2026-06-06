using Services.Interfaces;

namespace Services.StreamFactories;

public class StreamReaderFactory:IStreamReaderFactory
{
    public StreamReader Create(string path)
    {
        return new StreamReader(path);
    }
}