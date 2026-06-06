using Models;

namespace Services.Interfaces;

public interface ICsvGenerator
{
    Task OpenAsync(string outputPath);
    Task WriteRowAsync(ExcelModel model);
    Task CloseAsync();
}