using Microsoft.Extensions.Options;
using Models;
using Services.Interfaces;

namespace DigitalUsability;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelSettings _excelSettings;
    private readonly IHostApplicationLifetime _lifetime;

    public Worker(
        ILogger<Worker> logger,
        IServiceProvider serviceProvider,
        IOptions<ExcelSettings> excelOptions,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _excelSettings = excelOptions.Value;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation(
                "Worker started at: {time}",
                DateTimeOffset.Now);

            using (var scope = _serviceProvider.CreateScope())
            {
                var csvProcessor =
                    scope.ServiceProvider.GetRequiredService<ICsvProcessor>();

                await csvProcessor.ProcessAsync();
            }

            _logger.LogInformation(
                "Worker completed successfully at: {time}",
                DateTimeOffset.Now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in worker");
        }
        finally
        {
            _logger.LogInformation(
                "Worker stopping at: {time}",
                DateTimeOffset.Now);

            _lifetime.StopApplication();
        }
    }
}