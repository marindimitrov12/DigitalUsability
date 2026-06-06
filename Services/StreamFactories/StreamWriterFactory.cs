using System.Text;
using Services.Interfaces;

namespace Services.StreamFactories;

public class StreamWriterFactory:IStreamWriterFactory
{
    public StreamWriter Create(string path, bool append, Encoding encoding)
    {
        return new StreamWriter(path, append, encoding);
    }
}