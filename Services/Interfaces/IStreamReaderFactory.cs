namespace Services.Interfaces;

public interface IStreamReaderFactory
{
    StreamReader Create(string path);
}