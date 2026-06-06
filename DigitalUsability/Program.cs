using DigitalUsability;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Models;
using Services;
using Services.Interfaces;
using Serilog;
using Services.CsvGenerator;
using Services.CsvProcessor;
using Services.ExcelReader;
using Services.NetworkHelper;
using Services.StreamFactories;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

// Load configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Bind settings
builder.Services.Configure<ExcelSettings>(
    builder.Configuration.GetSection("ExcelSettings"));

// Register IExcelReader
builder.Services.AddTransient<IExcelReader>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<ExcelSettings>>().Value;
    var factory = sp.GetRequiredService<IStreamReaderFactory>();

    return new ExcelReader(
        settings.FilePathIps,
        factory);
});

// Register IExcelReader2
builder.Services.AddTransient<IExcelReader2>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<ExcelSettings>>().Value;
    var factory = sp.GetRequiredService<IStreamReaderFactory>();

    return new ExcelReader2(
        settings.FilePathGeo,
        factory);
});

// Register services
builder.Services.AddTransient<ICsvProcessor, CsvProcessor>();
builder.Services.AddTransient<INetworkHelper, NetworkHelper>();
builder.Services.AddTransient<ICsvGenerator, CsvGenerator>();
builder.Services.AddTransient<IStreamWriterFactory, StreamWriterFactory>();
builder.Services.AddTransient<IStreamReaderFactory, StreamReaderFactory>();

// Register worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();