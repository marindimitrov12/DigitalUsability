namespace Services.Interfaces;

public interface IExcelReader
{
    IAsyncEnumerable<string[]> ReadLinesAsync(char delimiter=',',bool skipHeader=true);
}