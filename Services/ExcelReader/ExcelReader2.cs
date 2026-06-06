using Services.Interfaces;

namespace Services.ExcelReader;

public class ExcelReader2:IExcelReader2
{
    private readonly string _filePath;
    private readonly IStreamReaderFactory _factory;

    public ExcelReader2(string filePath, IStreamReaderFactory factory)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        if(!File.Exists(filePath))
            throw new FileNotFoundException($"CSV file not found.", filePath);
        
        _filePath = filePath;
        _factory = factory;
    }
    public async IAsyncEnumerable<string[]> ReadLinesAsync(char delimiter = ',', bool skipHeader = true)
    {
        using var reader = _factory.Create(_filePath);

        string? line;

        if (skipHeader)
            await reader.ReadLineAsync();
        
        while ((line = await reader.ReadLineAsync()) != null)
        {
            yield return line.Split(delimiter);
        }
    }
}