using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Services.Interfaces;

namespace Services.CsvProcessor;

public class CsvProcessor : ICsvProcessor
{
    private readonly IExcelReader _reader;
    private readonly IExcelReader2 _reader2;
    private readonly INetworkHelper _networkHelper;
    private readonly ICsvGenerator _csv;
    private readonly ExcelSettings _settings;
    private readonly ILogger<CsvProcessor> _logger;

    public CsvProcessor(
        IExcelReader reader,
        INetworkHelper networkHelper,
        IExcelReader2 reader2,
        ICsvGenerator csv,
        IOptions<ExcelSettings> settings,
        ILogger<CsvProcessor> logger)
    {
        _reader = reader;
        _networkHelper = networkHelper;
        _reader2 = reader2;
        _csv = csv;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task ProcessAsync()
    {
        var countryMap = new Dictionary<string, CountryModel>();

        string fileName = "GeoLite_IP_Country.csv";
        var outputPath = _settings.OutputPath;
        string fullPath = Path.Combine(outputPath, fileName);

        await _csv.OpenAsync(fullPath);

        await foreach (var row in _reader2.ReadLinesAsync(skipHeader: true))
        {
            countryMap[row[0]] = new CountryModel
            {
                Name = row[5],
                Iso = row[4],
                ContinentName = row[3],
                ContinentCode = row[2]
            };
        }

        await foreach (var row in _reader.ReadLinesAsync())
        {
            var cidr = row[0];
            var geonameId = row[1];

            var (networkAddress, broadCastAddress) = GetIpRangeAddresses(cidr);
            var (hostIpFrom, hostIpTo) = GetHostIpRange(
                cidr,
                networkAddress,
                broadCastAddress);

            countryMap.TryGetValue(geonameId, out var model);

            var countryName = model?.Name ?? string.Empty;
            var iso = model?.Iso ?? string.Empty;
            var continentName = model?.ContinentName ?? string.Empty;
            var continentCode = model?.ContinentCode ?? string.Empty;

            var excelModel = ConvertRow(
                countryName,
                iso,
                continentName,
                continentCode,
                cidr,
                networkAddress,
                broadCastAddress,
                geonameId,
                hostIpFrom,
                hostIpTo);

            await _csv.WriteRowAsync(excelModel);
        }

        await _csv.CloseAsync();

        _logger.LogInformation(
            "CSV processing completed successfully at: {Path}",
            fullPath);
    }

    private ExcelModel ConvertRow(
        string countryName,
        string iso,
        string continentName,
        string continentCode,
        string cidr,
        string networkAddress,
        string broadCastAddress,
        string geonameId,
        string hostIpFrom,
        string hostIpTo)
    {
        return new ExcelModel
        {
            Network = cidr,
            GeonameId = geonameId,
            NetworkAddress = networkAddress,
            CountryISO = iso,
            BroadCastAddress = broadCastAddress,
            ContinentCode = continentCode,
            ContinentName = continentName,
            CountryName = countryName,
            HostIpRangeFrom = hostIpFrom,
            HostIpRangeTo = hostIpTo
        };
    }

    
    private (string networkAddress, string broadcastAddress)
        GetIpRangeAddresses(string cidr)
    {
        var (from, to) = _networkHelper.GetIpRange(cidr);
        return (from, to);
    }

    private (string hostIpFrom, string hostIpTo)
        GetHostIpRange(
            string cidr,
            string networkAddress,
            string broadcastAddress)
    {
        try
        {
            var prefixLength = ExtractPrefixLength(cidr);

            if (prefixLength == 32)
            {
                return (networkAddress, networkAddress);
            }

            if (prefixLength == 31)
            {
                return (networkAddress, broadcastAddress);
            }

            long networkIp = long.Parse(ConvertIpToLong(networkAddress));
            long broadcastIp = long.Parse(ConvertIpToLong(broadcastAddress));

            string hostIpFrom = ConvertLongToIp(networkIp + 1);
            string hostIpTo = ConvertLongToIp(broadcastIp - 1);

            return (hostIpFrom, hostIpTo);
        }
        catch
        {
            return (networkAddress, broadcastAddress);
        }
    }

    private int ExtractPrefixLength(string cidr)
    {
        if (string.IsNullOrWhiteSpace(cidr))
        {
            throw new ArgumentException(
                "CIDR cannot be null or empty",
                nameof(cidr));
        }

        var parts = cidr.Split('/');

        if (parts.Length != 2)
        {
            throw new ArgumentException(
                $"Invalid CIDR format: {cidr}. Expected format: xxx.xxx.xxx.xxx/xx");
        }

        if (!int.TryParse(parts[1], out int prefixLength))
        {
            throw new ArgumentException(
                $"Invalid prefix length in CIDR: {cidr}");
        }

        if (prefixLength < 0 || prefixLength > 32)
        {
            throw new ArgumentException(
                $"Prefix length must be between 0 and 32, got {prefixLength}");
        }

        return prefixLength;
    }

    private string ConvertIpToLong(string ipAddress)
    {
        var parts = ipAddress.Split('.');

        if (parts.Length != 4)
        {
            throw new ArgumentException(
                $"Invalid IP address: {ipAddress}");
        }

        long result = 0;

        for (int i = 0; i < 4; i++)
        {
            result = result * 256 + long.Parse(parts[i]);
        }

        return result.ToString();
    }

    private string ConvertLongToIp(long ipLong)
    {
        return
            $"{(ipLong >> 24) & 0xFF}." +
            $"{(ipLong >> 16) & 0xFF}." +
            $"{(ipLong >> 8) & 0xFF}." +
            $"{ipLong & 0xFF}";
    }
}