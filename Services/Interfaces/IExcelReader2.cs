namespace Services.Interfaces;

public interface IExcelReader2
{
    IAsyncEnumerable<string[]> ReadLinesAsync(char delimiter=',',bool skipHeader=true);
}