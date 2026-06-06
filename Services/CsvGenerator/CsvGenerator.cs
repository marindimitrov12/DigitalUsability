using System.Net;
using System.Text;
using Models;
using Services.Interfaces;

namespace Services.CsvGenerator;

public class CsvGenerator:ICsvGenerator
{
    private readonly IStreamWriterFactory _writerFactory;
    private StreamWriter _writer;
 
    public CsvGenerator(IStreamWriterFactory writerFactory)
    {
        _writerFactory = writerFactory;
    }  

    public async Task OpenAsync(string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
 
 
 
        _writer = _writerFactory.Create(
            outputPath,
            append:false,
            encoding:Encoding.UTF8);
 
 
        await _writer.WriteLineAsync(
            "Network;Network Address;Broadcast Address;Host IP Range From;Host IP Range To;Geoname ID;Continent Code;Continent Name;Country ISO;Country Name"
        );
    }

    public async Task WriteRowAsync(ExcelModel model)
    {
        if (_writer == null)
        {
            throw new InvalidOperationException("CSV writer is not opened. Call OpenAsync() before writing rows.");
        }
        var line = string.Join(";",
            Escape(model.Network),
            Escape(model.NetworkAddress),
            Escape(model.BroadCastAddress),
            Escape(model.HostIpRangeFrom),
            Escape(model.HostIpRangeTo),
            Escape(model.GeonameId),
            Escape(model.ContinentCode),
            Escape(model.ContinentName),
            Escape(model.CountryISO),
            Escape(model.CountryName)
        );
 
        await _writer.WriteLineAsync(line);
    }
 
    public async Task CloseAsync()
    {
        if(_writer == null)
            return; 
        await _writer.FlushAsync();
        _writer.Dispose();
        _writer= null;
    }
 
    private static string Escape(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
 
 
        value = value.Trim().Trim('"');
 
 
        if (value.Contains(";") || value.Contains("\""))
            return $"\"{value.Replace("\"", "\"\"")}\"";
 
        return value;
    }


}