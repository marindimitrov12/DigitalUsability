using System.Text;

namespace Services.Interfaces;

public interface IStreamWriterFactory
{
    StreamWriter Create(string path,bool append, Encoding encoding);
}